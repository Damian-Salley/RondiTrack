using Microsoft.AspNetCore.Mvc;
using RondiTrack.DTOs.ContributionCycles;
using RondiTrack.Exceptions;
using RondiTrack.Mappers;
using RondiTrack.Repositories;

namespace RondiTrack.Controllers;

[ApiController]
[Route("api/contribution-cycles")]
public class ContributionCyclesController : ControllerBase
{
    private readonly IRondiTrackRepository _repository;

    public ContributionCyclesController(
        IRondiTrackRepository repository)
    {
        _repository = repository;
    }

    // Get all contribution cycles
    [HttpGet]
    [EndpointSummary("Get all contribution cycles")]
    [EndpointDescription("Returns all contribution cycles currently stored in RondiTrack.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ContributionCycleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ContributionCycleResponse>>>
        GetContributionCycles()
    {
        var cycles =
            await _repository.GetContributionCyclesAsync();

        var responses = new List<ContributionCycleResponse>();

        foreach (var cycle in cycles)
        {
            responses.Add(
                ContributionCycleMapper.ToResponse(cycle));
        }

        return Ok(responses);
    }

    // Get contribution cycle by ID
    [HttpGet("{id:int}")]
    [EndpointSummary("Get a contribution cycle by ID")]
    [EndpointDescription("Returns the contribution cycle with the specified ID.")]
    [ProducesResponseType(typeof(ContributionCycleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContributionCycleResponse>>
         GetContributionCycle(int id)
    {
        var cycle =
            await _repository.GetContributionCycleByIdAsync(id);

        if (cycle is null)
        {
            throw new NotFoundException(
                $"No contribution cycle with ID {id} was found.");
        }

        return Ok(
            ContributionCycleMapper.ToResponse(cycle));
    }

    // Create contribution cycle
    [HttpPost]
    [EndpointSummary("Create a contribution cycle")]
    [EndpointDescription("Creates a contribution cycle for an existing stokvel.")]
    [ProducesResponseType(typeof(ContributionCycleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContributionCycleResponse>>
        CreateContributionCycle(
            CreateContributionCycleRequest request)
    {
        var stokvel =
            await _repository.GetStokvelByIdAsync(request.StokvelId);

        if (stokvel is null)
        {
            throw new NotFoundException(
                $"No stokvel with ID {request.StokvelId} was found.");
        }

        var existingCycle =
            await _repository.GetContributionCycleByIdAsync(request.Id);

        if (existingCycle is not null)
        {
            throw new BusinessRuleException(
                $"A contribution cycle with ID {request.Id} already exists.");
        }

        var cycle =
            ContributionCycleMapper.ToDomain(request);

        await _repository.AddContributionCycleAsync(cycle);

        var response =
            ContributionCycleMapper.ToResponse(cycle);

        return CreatedAtAction(
            nameof(GetContributionCycle),
            new { id = cycle.Id },
            response);
    }

    // Update contribution cycle
    [HttpPut("{id:int}")]
    [EndpointSummary("Update a contribution cycle")]
    [EndpointDescription("Updates the number and target amount of an existing contribution cycle.")]
    [ProducesResponseType(typeof(ContributionCycleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContributionCycleResponse>>
         UpdateContributionCycle(
             int id,
             UpdateContributionCycleRequest request)
    {
        var cycle =
            await _repository.GetContributionCycleByIdAsync(id);

        if (cycle is null)
        {
            throw new NotFoundException(
                $"No contribution cycle with ID {id} was found.");
        }

        ContributionCycleMapper.Update(
            cycle,
            request);

        await _repository.UpdateContributionCycleAsync(cycle);

        return Ok(
            ContributionCycleMapper.ToResponse(cycle));
    }

    // Delete contribution cycle
    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete a contribution cycle")]
    [EndpointDescription("Deletes the contribution cycle with the specified ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
          DeleteContributionCycle(int id)
    {
        var deleted =
            await _repository.DeleteContributionCycleAsync(id);

        if (!deleted)
        {
            throw new NotFoundException(
                $"No contribution cycle with ID {id} was found.");
        }

        return NoContent();
    }
}