using Microsoft.AspNetCore.Mvc;
using RondiTrack.Models;
using RondiTrack.Repositories;

namespace RondiTrack.Controllers;

[ApiController]
[Route("api/stokvels")]
public class StokvelsController : ControllerBase
{
    //Dependency injection of the repository
    private readonly IRondiTrackRepository _repository;

    //Constructor to inject the repository
    public StokvelsController(IRondiTrackRepository repository)
    {
        _repository = repository;
    }

    //CRUD Operations for Stokvel
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Stokvel>>> GetStokvels()
    {
        var stokvels = await _repository.GetStokvelsAsync();
        return Ok(stokvels);
    }

    //Get a stokvel by ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Stokvel>> GetStokvel(int id)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(id);

        if (stokvel is null)
        {
            return NotFound();
        }

        return Ok(stokvel);
    }

    //Create a new stokvel
    [HttpPost]
    public async Task<ActionResult<Stokvel>> CreateStokvel(Stokvel stokvel)
    {
        var existingStokvel = await _repository.GetStokvelByIdAsync(stokvel.Id);

        if (existingStokvel is not null)
        {
            return Conflict("A stokvel with this ID already exists.");
        }

        await _repository.AddStokvelAsync(stokvel);

        return CreatedAtAction(
            nameof(GetStokvel),
            new { id = stokvel.Id },
            stokvel);
    }

    //Update an existing stokvel
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Stokvel>> UpdateStokvel(int id, Stokvel updatedStokvel)
    {
        if (id != updatedStokvel.Id)
        {
            return BadRequest("The ID in the URL must match the stokvel's ID.");
        }

        var existingStokvel = await _repository.GetStokvelByIdAsync(id);

        if (existingStokvel is null)
        {
            return NotFound();
        }

        existingStokvel.UpdateDetails(
            updatedStokvel.Name,
            updatedStokvel.ContributionAmount);

        await _repository.UpdateStokvelAsync(existingStokvel);

        return Ok(existingStokvel);
    }

    //Delete a stokvel by ID
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStokvel(int id)
    {
        var deleted = await _repository.DeleteStokvelAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    //Get members of a specific stokvel
    [HttpGet("{stokvelId:int}/members")]
    public async Task<ActionResult<IReadOnlyCollection<User>>> GetStokvelMembers(int stokvelId)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return NotFound();
        }

        return Ok(stokvel.Members);
    }

    //Add a member to a specific stokvel
    [HttpPost("{stokvelId:int}/members/{userId:int}")]
    public async Task<IActionResult> AddMember(int stokvelId, int userId)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return NotFound();
        }

        var user = await _repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            return NotFound();
        }

        try
        {
            stokvel.AddMember(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }

        return Ok();
    }

     //Delete a user by ID
    [HttpDelete("{stokvelId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int stokvelId, int userId)
    {
       var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return NotFound();
        }

        var user = await _repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            return NotFound();
        }

        try
        {
            stokvel.RemoveMember(user);
        }
        catch (InvalidOperationException)
        {
            return NoContent();
        }
        return Ok();
    }
}