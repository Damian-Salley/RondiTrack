using Microsoft.AspNetCore.Mvc;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.DTOs.Users;
using RondiTrack.DTOs.Contributions;
using RondiTrack.Mappers;
using RondiTrack.Repositories;
using RondiTrack.Services;
using RondiTrack.Exceptions;

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

    // Get all stokvels
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StokvelResponse>>> GetStokvels()
    {
        var stokvels = await _repository.GetStokvelsAsync();

        var responses = new List<StokvelResponse>();

        foreach (var stokvel in stokvels)
        {
            responses.Add(StokvelMapper.ToResponse(stokvel));
        }

        return Ok(responses);
    }

    // Get a stokvel by ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StokvelResponse>> GetStokvel(int id)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(id);

        if (stokvel is null)
        {
            throw new NotFoundException(
                $"No stokvel with ID {id} was found.");
        }

        return Ok(StokvelMapper.ToResponse(stokvel));
    }

    // Create a stokvel
    [HttpPost]
    public async Task<ActionResult<StokvelResponse>> CreateStokvel(
        CreateStokvelRequest request)
    {
        var existingStokvel =
            await _repository.GetStokvelByIdAsync(request.Id);

        if (existingStokvel is not null)
        {
            throw new BusinessRuleException(
                $"A stokvel with ID {request.Id} already exists.");
        }

        var stokvel = StokvelMapper.ToDomain(request);

        await _repository.AddStokvelAsync(stokvel);

        var response = StokvelMapper.ToResponse(stokvel);

        return CreatedAtAction(
            nameof(GetStokvel),
            new { id = stokvel.Id },
            response);
    }

    // Update a stokvel
    [HttpPut("{id:int}")]
    public async Task<ActionResult<StokvelResponse>> UpdateStokvel(
        int id,
        UpdateStokvelRequest request)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(id);

        if (stokvel is null)
        {
            throw new NotFoundException(
                $"No stokvel with ID {id} was found.");
        }

        StokvelMapper.Update(stokvel, request);

        await _repository.UpdateStokvelAsync(stokvel);

        return Ok(StokvelMapper.ToResponse(stokvel));
    }

    // Delete a stokvel
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStokvel(int id)
    {
        var deleted = await _repository.DeleteStokvelAsync(id);

        if (!deleted)
        {
            throw new NotFoundException(
                $"No stokvel with ID {id} was found.");
        }

        return NoContent();
    }

    // Get members of a stokvel
    [HttpGet("{stokvelId:int}/members")]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>>
        GetStokvelMembers(int stokvelId)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            throw new NotFoundException(
                $"No stokvel with ID {stokvelId} was found.");
        }

        var responses = new List<UserResponse>();

        foreach (var member in stokvel.Members)
        {
            responses.Add(UserMapper.ToResponse(member));
        }

        return Ok(responses);
    }

    // Add a member to a stokvel
    [HttpPost("{stokvelId:int}/members/{userId:int}")]
    public async Task<IActionResult> AddMember(
        int stokvelId,
        int userId)
    {
        await _service.AddMemberAsync(stokvelId, userId);

        return Ok();
    }

    // Remove a member from a stokvel
    [HttpDelete("{stokvelId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(
        int stokvelId,
        int userId)
    {
        await _service.RemoveMemberAsync(stokvelId, userId);

        return NoContent();
    }

   // Record a contribution
[HttpPost("{stokvelId:int}/members/{userId:int}/contributions")]
public async Task<ActionResult<ContributionResponse>> RecordContribution(
    int stokvelId,
    int userId,
    RecordContributionRequest request,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
{
    if (string.IsNullOrWhiteSpace(idempotencyKey))
    {
        return Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Idempotency key required",
            detail: "The Idempotency-Key header is required.");
    }

    var contribution = await _service.RecordContributionAsync(
        stokvelId,
        userId,
        request.Cycle,
        idempotencyKey);

    return Ok(ContributionMapper.ToResponse(contribution));
    }
}