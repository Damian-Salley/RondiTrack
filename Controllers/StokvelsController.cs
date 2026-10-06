using Microsoft.AspNetCore.Mvc;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.DTOs.Users;
using RondiTrack.Mappers;
using RondiTrack.Models;
using RondiTrack.Repositories;
using RondiTrack.Services;
using RondiTrack.DTOs.Pagination;

namespace RondiTrack.Controllers;

[ApiController]
[Route("api/stokvels")]
public class StokvelsController : ControllerBase
{
    private readonly IRondiTrackRepository _repository;
    private readonly IRondiTrackService _service;

    public StokvelsController(
        IRondiTrackRepository repository,
        IRondiTrackService service)
    {
        _repository = repository;
        _service = service;
    }


    // =========================================================
    // STOKVEL OPERATIONS
    // =========================================================

    //Get all stokvels
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StokvelResponse>>> GetStokvels()
    {
        var stokvels =
            await _repository.GetStokvelsAsync();

        var responses = stokvels
            .Select(StokvelMapper.ToResponse)
            .ToList();

        return Ok(responses);
    }


    //Get stokvel by ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StokvelResponse>> GetStokvelById(int id)
    {
        var stokvel =
            await _repository.GetStokvelByIdAsync(id);

        if (stokvel is null)
        {
            return NotFound();
        }

        return Ok(
            StokvelMapper.ToResponse(stokvel));
    }


    //Create a new stokvel
    [HttpPost]
    public async Task<ActionResult<StokvelResponse>> CreateStokvel(
        CreateStokvelRequest request)
    {
        var stokvel =
            StokvelMapper.ToDomain(request);

        await _repository.AddStokvelAsync(stokvel);

        var response =
            StokvelMapper.ToResponse(stokvel);

        return CreatedAtAction(
            nameof(GetStokvelById),
            new { id = stokvel.Id },
            response);
    }


    //Update a stokvel
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStokvel(
        int id,
        UpdateStokvelRequest request)
    {
        var stokvel =
            await _repository.GetStokvelByIdAsync(id);

        if (stokvel is null)
        {
            return NotFound();
        }

        StokvelMapper.Update(
            stokvel,
            request);

        var updated =
            await _repository.UpdateStokvelAsync(stokvel);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }


    //Delete a stokvel
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStokvel(int id)
    {
        var deleted =
            await _repository.DeleteStokvelAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }


    // =========================================================
    // MEMBER OPERATIONS
    // =========================================================

    //Get all members of a stokvel
    [HttpGet("{stokvelId:int}/members")]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetStokvelMembers(
        int stokvelId)
    {
        var stokvel =
            await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return NotFound();
        }

        var responses = new List<UserResponse>();

        foreach (var member in stokvel.Members)
        {
            responses.Add(
                UserMapper.ToResponse(member.User));
        }

        return Ok(responses);
    }


    //Add a member to a stokvel
    [HttpPost("{stokvelId:int}/members/{userId:int}")]
    public async Task<IActionResult> AddMember(
        int stokvelId,
        int userId)
    {
        await _service.AddMemberAsync(
            stokvelId,
            userId);

        return NoContent();
    }


    //Remove a member from a stokvel
    [HttpDelete("{stokvelId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(
        int stokvelId,
        int userId)
    {
        await _service.RemoveMemberAsync(
            stokvelId,
            userId);

        return NoContent();
    }


    // =========================================================
    // CONTRIBUTION OPERATIONS
    // =========================================================

    //Record a contribution
    [HttpPost("{stokvelId:int}/members/{userId:int}/contributions")]
    public async Task<ActionResult<ContributionResponse>> RecordContribution(
        int stokvelId,
        int userId,
        RecordContributionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        var contribution =
            await _service.RecordContributionAsync(
                stokvelId,
                userId,
                request.Cycle,
                idempotencyKey);

        return Ok(
            ContributionMapper.ToResponse(contribution));
    }
//Get filtered, sorted and paged contributions for a cycle
[HttpGet("{stokvelId:int}/cycles/{cycleId:int}/contributions")]
public async Task<ActionResult<PagedResponse<ContributionResponse>>>
    GetContributionsByCycle(
        int stokvelId,
        int cycleId,
        [FromQuery] ContributionPageRequest request)
{
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


    // =========================================================
    // PAYOUT OPERATIONS
    // =========================================================

    //Process a payout
    [HttpPost("{stokvelId:int}/cycles/{contributionCycleId:int}/payout")]
    public async Task<ActionResult<Payout>> ProcessPayout(
        int stokvelId,
        int contributionCycleId)
    {
        var payout =
            await _service.ProcessPayoutAsync(
                stokvelId,
                contributionCycleId);

        return Ok(payout);
    }
}