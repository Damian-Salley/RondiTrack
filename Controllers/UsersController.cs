using Microsoft.AspNetCore.Mvc;
using RondiTrack.Models;
using RondiTrack.Repositories;

namespace RondiTrack.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IRondiTrackRepository _repository;

    public UsersController(IRondiTrackRepository repository)
    {
        _repository = repository;
    }

    //CRUD Operations for User
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<User>>> GetUsers()
    {
        var users = await _repository.GetUsersAsync();
        return Ok(users);
    }

    //Get a user by ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _repository.GetUserByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    //Create a new user
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        var existingUser = await _repository.GetUserByIdAsync(user.Id);

        if (existingUser is not null)
        {
            return Conflict("A user with this ID already exists.");
        }

        await _repository.AddUserAsync(user);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    //Update an existing user
    [HttpPut("{id:int}")]
    public async Task<ActionResult<User>> UpdateUser(int id, User updatedUser)
    {
        if (id != updatedUser.Id)
        {
            return BadRequest("The ID in the URL must match the user's ID.");
        }

        var existingUser = await _repository.GetUserByIdAsync(id);

        if (existingUser is null)
        {
            return NotFound();
        }

        existingUser.UpdateDetails(
            updatedUser.FirstName,
            updatedUser.LastName,
            updatedUser.Email,
            updatedUser.PhoneNumber);

        await _repository.UpdateUserAsync(existingUser);

        return Ok(existingUser);
    }

    //Delete a user by ID
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var deleted = await _repository.DeleteUserAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

