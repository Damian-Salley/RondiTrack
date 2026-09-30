using Microsoft.AspNetCore.Mvc;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.DTOs.Users;
using RondiTrack.DTOs.Contributions;
using RondiTrack.Mappers;
using RondiTrack.Repositories;
using RondiTrack.Services;
using RondiTrack.Exceptions;
using RondiTrack.Models;

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
    [EndpointSummary("Get all stokvels")]
    [EndpointDescription("Returns all stokvels currently stored in RondiTrack.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<StokvelResponse>), StatusCodes.Status200OK)]
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
    [EndpointSummary("Get a stokvel by ID")]
    [EndpointDescription("Returns the stokvel with the specified ID.")]
    [ProducesResponseType(typeof(StokvelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Create a stokvel")]
    [EndpointDescription("Creates a new stokvel. The request must be valid and its ID must not already exist.")]
    [ProducesResponseType(typeof(StokvelResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
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
    [EndpointSummary("Update a stokvel")]
    [EndpointDescription("Updates an existing stokvel.")]
    [ProducesResponseType(typeof(StokvelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Delete a stokvel")]
    [EndpointDescription("Deletes the stokvel with the specified ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Get stokvel members")]
    [EndpointDescription("Returns all users who are members of the specified stokvel.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Add a member to a stokvel")]
    [EndpointDescription("Adds an existing user to an existing stokvel. A user who is already a member cannot be added again.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddMember(
          int stokvelId,
          int userId)
    {
        await _service.AddMemberAsync(stokvelId, userId);

        return Ok();
    }

    // Remove a member from a stokvel
    [HttpDelete("{stokvelId:int}/members/{userId:int}")]
    [EndpointSummary("Remove a member from a stokvel")]
    [EndpointDescription("Removes an existing member from the specified stokvel.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveMember(
        int stokvelId,
        int userId)
    {
        await _service.RemoveMemberAsync(stokvelId, userId);

        return NoContent();
    }

    // Record a contribution
    [HttpPost("{stokvelId:int}/members/{userId:int}/contributions")]
    [EndpointSummary("Record a contribution")]
    [EndpointDescription("Records a member contribution against an existing contribution cycle. Requires an Idempotency-Key header.")]
    [ProducesResponseType(typeof(ContributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContributionResponse>> RecordContribution(
          int stokvelId,
          int userId,
          RecordContributionRequest request,
          [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new RequestValidationException(
                "The Idempotency-Key header is required.");
        }

        var contribution = await _service.RecordContributionAsync(
            stokvelId,
            userId,
            request.Cycle,
            idempotencyKey);

        return Ok(ContributionMapper.ToResponse(contribution));

    }
    // Process a payout
    [HttpPost("{stokvelId:int}/cycles/{contributionCycleId:int}/payout")]
    [EndpointSummary("Process a payout")]
    [EndpointDescription("Processes the payout for a contribution cycle and marks the cycle as paid.")]
    [ProducesResponseType(typeof(Payout), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Payout>> ProcessPayout(
        int stokvelId,
        int contributionCycleId)
    {
        var payout = await _service.ProcessPayoutAsync(
            stokvelId,
            contributionCycleId);

        return Ok(payout);
    }

}