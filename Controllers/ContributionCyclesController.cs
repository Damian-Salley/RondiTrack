using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RondiTrack.DTOs.ContributionCycles;
using RondiTrack.Exceptions;
using RondiTrack.Mappers;
using RondiTrack.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RondiTrack.Controllers;

[ApiController]
[Authorize]
[Route("api/contribution-cycles")]
public class ContributionCyclesController : ControllerBase
{
    private readonly IRondiTrackRepository _repository;
    private readonly IAuthorizationService _authorization;

    public ContributionCyclesController(
        IRondiTrackRepository repository,
        IAuthorizationService authorization)
    {
        _repository = repository;
        _authorization = authorization;
    }

    // Get the authenticated user's ID.
    private int? CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var id) ? id : null;
        }
    }

    // Non-member = 404. Member without permission = 403.
    private async Task<ActionResult?> CheckAccessAsync(
        int stokvelId,
        bool elevated = false)
    {
        var memberAccess = await _authorization.AuthorizeAsync(
            User, stokvelId, "StokvelMember");

        if (!memberAccess.Succeeded)
            return NotFound();

        if (elevated)
        {
            var managerAccess = await _authorization.AuthorizeAsync(
                User, stokvelId, "StokvelManager");

            if (!managerAccess.Succeeded)
                return Forbid();
        }

        return null;
    }

    // Return only cycles belonging to the authenticated user's stokvels.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ContributionCycleResponse>>>
        GetContributionCycles()
    {
        if (CurrentUserId is not int userId)
            return Unauthorized();

        var cycles = await _repository.GetContributionCyclesAsync();

        var responses = new List<ContributionCycleResponse>();

        foreach (var cycle in cycles)
        {
            var membership = await _repository.GetMemberForContributionAsync(
                cycle.StokvelId,
                userId);

            if (membership is not null)
            {
                responses.Add(
                    ContributionCycleMapper.ToResponse(cycle));
            }
        }

        return Ok(responses);
    }

    // Members can view cycles belonging to their stokvel.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContributionCycleResponse>>
        GetContributionCycle(int id)
    {
        var cycle = await _repository.GetContributionCycleByIdAsync(id);

        if (cycle is null)
            return NotFound();

        var accessFailure = await CheckAccessAsync(cycle.StokvelId);

        if (accessFailure is not null)
            return accessFailure;

        return Ok(ContributionCycleMapper.ToResponse(cycle));
    }

    // Treasurer or Chairperson can create cycles.
    [HttpPost]
    public async Task<ActionResult<ContributionCycleResponse>>
        CreateContributionCycle(CreateContributionCycleRequest request)
    {
        var accessFailure = await CheckAccessAsync(
            request.StokvelId,
            elevated: true);

        if (accessFailure is not null)
            return accessFailure;

        var stokvel = await _repository.GetStokvelByIdAsync(request.StokvelId);

        if (stokvel is null)
            return NotFound();

        var existingCycle = await _repository.GetContributionCycleByIdAsync(
            request.Id);

        if (existingCycle is not null)
        {
            throw new BusinessRuleException(
                $"A contribution cycle with ID {request.Id} already exists.");
        }

        var cycle = ContributionCycleMapper.ToDomain(request);

        await _repository.AddContributionCycleAsync(cycle);

        return CreatedAtAction(
            nameof(GetContributionCycle),
            new { id = cycle.Id },
            ContributionCycleMapper.ToResponse(cycle));
    }

    // Treasurer or Chairperson can update cycles.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContributionCycleResponse>>
        UpdateContributionCycle(
            int id,
            UpdateContributionCycleRequest request)
    {
        var cycle = await _repository.GetContributionCycleByIdAsync(id);

        if (cycle is null)
            return NotFound();

        var accessFailure = await CheckAccessAsync(
            cycle.StokvelId,
            elevated: true);

        if (accessFailure is not null)
            return accessFailure;

        ContributionCycleMapper.Update(cycle, request);

        await _repository.UpdateContributionCycleAsync(cycle);

        return Ok(ContributionCycleMapper.ToResponse(cycle));
    }

    // Treasurer or Chairperson can delete cycles.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteContributionCycle(int id)
    {
        var cycle = await _repository.GetContributionCycleByIdAsync(id);

        if (cycle is null)
            return NotFound();

        var accessFailure = await CheckAccessAsync(
            cycle.StokvelId,
            elevated: true);

        if (accessFailure is not null)
            return accessFailure;

        var deleted = await _repository.DeleteContributionCycleAsync(id);

        return deleted ? NoContent() : NotFound();
    }
}