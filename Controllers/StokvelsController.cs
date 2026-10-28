using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.Pagination;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.DTOs.Users;
using RondiTrack.Mappers;
using RondiTrack.Models;
using RondiTrack.Repositories;
using RondiTrack.Services;
using RondiTrack.Idempotency;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RondiTrack.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace RondiTrack.Controllers;

[ApiController]
[Authorize]
[Route(ApiRoutes.Prefix + "/stokvels")]
public class StokvelsController : ControllerBase
{
    private readonly IRondiTrackRepository _repository;
    private readonly IRondiTrackService _service;
    private readonly IAuthorizationService _authorization;
    // Check whether a contribution request has already been processed.
    private readonly IIdempotencyStore _idempotencyStore;

    public StokvelsController(
    IRondiTrackRepository repository,
    IRondiTrackService service,
    IAuthorizationService authorization,
    IIdempotencyStore idempotencyStore)
    {
        _repository = repository;
        _service = service;
        _authorization = authorization;
        _idempotencyStore = idempotencyStore;
    }

    // Get the authenticated user's ID from their JWT.
    private int? CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var id) ? id : null;
        }
    }



    // A non-member gets 404; a member lacking elevated permission gets 403.
    // Both decisions are made by the resource-based authorization handler.
    private async Task<ActionResult?> CheckAccessAsync(
     int stokvelId,
     bool elevated = false,
     bool chairperson = false)
    {
        var memberAccess = await _authorization.AuthorizeAsync(
            User, stokvelId, "StokvelMember");

        if (!memberAccess.Succeeded)
            return NotFound();

        if (chairperson)
        {
            var chairpersonAccess = await _authorization.AuthorizeAsync(
                User, stokvelId, "StokvelChairperson");

            if (!chairpersonAccess.Succeeded)
                return Forbid();
        }
        else if (elevated)
        {
            var managerAccess = await _authorization.AuthorizeAsync(
                User, stokvelId, "StokvelManager");

            if (!managerAccess.Succeeded)
                return Forbid();
        }

        return null;
    }

    // =========================================================
    // STOKVEL OPERATIONS
    // =========================================================

    // Return only stokvels belonging to the authenticated user.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StokvelResponse>>> GetStokvels()
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        var stokvels = await _repository.GetStokvelsAsync();

        var responses = stokvels
            .Where(stokvel => stokvel.Members.Any(
                member => member.UserId == userId))
            .Select(StokvelMapper.ToResponse)
            .ToList();

        return Ok(responses);
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyCollection<StokvelResponse>>> GetAllStokvels()
    {
        var stokvels = await _repository.GetStokvelsAsync();

        var responses = stokvels
            .Select(StokvelMapper.ToResponse)
            .ToList();

        return Ok(responses);
    }

    // Members can view their stokvel.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StokvelResponse>> GetStokvelById(int id)
    {
        var accessFailure = await CheckAccessAsync(id);
        if (accessFailure is not null)
            return accessFailure;

        var stokvel = await _repository.GetStokvelByIdAsync(id);

        if (stokvel is null)
            return NotFound();

        return Ok(StokvelMapper.ToResponse(stokvel));
    }

    // Create a stokvel and assign its creator as Chairperson.
    [HttpPost]
    public async Task<ActionResult<StokvelResponse>> CreateStokvel(
        CreateStokvelRequest request)
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        var user = await _repository.GetUserByIdAsync(userId);

        if (user is null)
            return Unauthorized();

        var stokvel = StokvelMapper.ToDomain(request);

        // Add the creator before saving the stokvel.
        stokvel.AddMember(user, "Chairperson");

        await _repository.AddStokvelAsync(stokvel);

        var response = StokvelMapper.ToResponse(stokvel);

        return CreatedAtAction(
            nameof(GetStokvelById),
            new { id = stokvel.Id },
            response);
    }

    // Only the Chairperson can update stokvel details.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStokvel(
        int id,
        UpdateStokvelRequest request)
    {
        var accessFailure = await CheckAccessAsync(id, chairperson: true);
        if (accessFailure is not null)
            return accessFailure;

        var stokvel = await _repository.GetStokvelByIdAsync(id);

        if (stokvel is null)
            return NotFound();

        StokvelMapper.Update(stokvel, request);

        var updated = await _repository.UpdateStokvelAsync(stokvel);

        return updated ? NoContent() : NotFound();
    }

    // Only the Chairperson can delete a stokvel.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStokvel(int id)
    {
        var accessFailure = await CheckAccessAsync(id, chairperson: true);
        if (accessFailure is not null)
            return accessFailure;

        var deleted = await _repository.DeleteStokvelAsync(id);

        return deleted ? NoContent() : NotFound();
    }

    // =========================================================
    // MEMBER OPERATIONS
    // =========================================================

    // Members can view other members of their stokvel.
    [HttpGet("{stokvelId:int}/members")]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetStokvelMembers(
        int stokvelId)
    {
        var accessFailure = await CheckAccessAsync(stokvelId);
        if (accessFailure is not null)
            return accessFailure;

        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
            return NotFound();

        var responses = stokvel.Members
            .Select(member => UserMapper.ToResponse(member.User))
            .ToList();

        return Ok(responses);
    }

    // Only the Chairperson can add members.
    [HttpPost("{stokvelId:int}/members/{userId:int}")]
    public async Task<IActionResult> AddMember(
        int stokvelId,
        int userId)
    {
        var accessFailure = await CheckAccessAsync(stokvelId, chairperson: true);
        if (accessFailure is not null)
            return accessFailure;


        await _service.AddMemberAsync(stokvelId, userId);

        return NoContent();
    }

    // Only the Chairperson can remove members.
    [HttpDelete("{stokvelId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(
        int stokvelId,
        int userId)
    {
        var accessFailure = await CheckAccessAsync(stokvelId, chairperson: true);
        if (accessFailure is not null)
            return accessFailure;


        // Prevent the Chairperson from removing themselves.
        if (CurrentUserId == userId)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Cannot remove yourself"
            });
        }

        await _service.RemoveMemberAsync(stokvelId, userId);

        return NoContent();
    }

    // =========================================================
    // CONTRIBUTION OPERATIONS
    // =========================================================

    // Existing endpoint retained for compatibility with previous assignments.
    // Members can record their own contributions.
    // Treasurer and Chairperson can record contributions for others.
    [EnableRateLimiting("Financial")]
    [HttpPost("{stokvelId:int}/members/{userId:int}/contributions")]
    public async Task<ActionResult<ContributionResponse>> RecordContribution(
        int stokvelId,
        int userId,
        RecordContributionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        var accessFailure = await CheckAccessAsync(
            stokvelId, elevated: CurrentUserId != userId);

        if (accessFailure is not null)
            return accessFailure;

        var contribution = await _service.RecordContributionAsync(
            stokvelId,
            userId,
            request.Cycle,
            idempotencyKey);

        return Ok(ContributionMapper.ToResponse(contribution));
    }

    // Assignment 7.4: Record a contribution using integer cents.
    [EnableRateLimiting("Financial")]
    [HttpPost("{stokvelId:int}/cycles/{cycleId:int}/contributions")]
    public async Task<ActionResult<ContributionResponse>> RecordCycleContribution(
    int stokvelId,
    int cycleId,
    RecordCycleContributionRequest request,
// Require an idempotency key for safe contribution retries.
    [FromHeader(Name = "Idempotency-Key")]
    [System.ComponentModel.DataAnnotations.Required]
    string idempotencyKey)
    {
        if (CurrentUserId is not int currentUserId)
            return Unauthorized();

        // Only stokvel members can record contributions.
        var accessFailure = await CheckAccessAsync(stokvelId);

        if (accessFailure is not null)
            return accessFailure;

        // Only Treasurers can record contributions for other members.
        if (currentUserId != request.UserId)
        {
            var member = await _repository.GetMemberForContributionAsync(
                stokvelId, currentUserId);

            if (member?.Role != "Treasurer")
                return Forbid();
        }

        // Check whether this idempotency key was already processed.
        var existingRecord = await _idempotencyStore.GetAsync(idempotencyKey);

        // Record the contribution or return the existing result.
        var contribution = await _service.RecordContributionAsync(
            stokvelId,
            request.UserId,
            cycleId,
            request.AmountInCents,
            idempotencyKey);

        // Tell the frontend whether the request was replayed.
        Response.Headers["Idempotency-Replayed"] =
            existingRecord is not null ? "true" : "false";

        return StatusCode(
            StatusCodes.Status201Created,
            ContributionMapper.ToResponse(contribution));
    }

    // Members can view contributions for their stokvel.
    [HttpGet("{stokvelId:int}/cycles/{cycleId:int}/contributions")]
    public async Task<ActionResult<PagedResponse<ContributionResponse>>>
        GetContributionsByCycle(
            int stokvelId,
            int cycleId,
            [FromQuery] ContributionPageRequest request)
    {
        var accessFailure = await CheckAccessAsync(stokvelId);

        if (accessFailure is not null)
            return accessFailure;

        try
        {
            var response =
                await _service.GetPagedContributionResponsesByCycleAsync(
                    stokvelId,
                    cycleId,
                    request);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid paging request",
                Detail = exception.Message
            });
        }
    }
    // GET /api/v1/stokvels/{stokvelId}/contributions
    // Returns paginated contributions for the stokvel's active cycle.
    [HttpGet("{stokvelId:int}/contributions")]
    public async Task<ActionResult<PagedResponse<ContributionResponse>>>
        GetCurrentCycleContributions(
            int stokvelId,
            [FromQuery] ContributionPageRequest request)
    {
        // Only authenticated members can view contributions.
        var accessFailure = await CheckAccessAsync(stokvelId);

        if (accessFailure is not null)
            return accessFailure;

        // Retrieve the existing contribution cycles.
        var cycles = await _repository.GetContributionCyclesAsync();

        // Find the active cycle for this stokvel.
        var activeCycle = cycles.FirstOrDefault(cycle =>
            cycle.StokvelId == stokvelId &&
            cycle.Status == "Active");

        // Return an empty page if there is no active cycle.
        if (activeCycle is null)
        {
            return Ok(new PagedResponse<ContributionResponse>
            {
                Items = new List<ContributionResponse>(),
                NextPageToken = null
            });
        }

        try
        {
            // IMPORTANT:
            // Contributions reference the cycle's database ID,
            // not its displayed cycle number.
            var response =
                await _service.GetPagedContributionResponsesByCycleAsync(
                    stokvelId,
                    activeCycle.Id,
                    request);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid paging request",
                Detail = exception.Message
            });
        }
    }

    // =========================================================
    // PAYOUT OPERATIONS
    // =========================================================

    // Only Treasurer and Chairperson can process payouts.
    [EnableRateLimiting("Financial")]
    [HttpPost("{stokvelId:int}/cycles/{contributionCycleId:int}/payout")]
    public async Task<ActionResult<Payout>> ProcessPayout(
        int stokvelId,
        int contributionCycleId)
    {
        var accessFailure = await CheckAccessAsync(stokvelId, elevated: true);
        if (accessFailure is not null)
            return accessFailure;

        var payout = await _service.ProcessPayoutAsync(
            stokvelId,
            contributionCycleId);

        return Ok(payout);
    }
}