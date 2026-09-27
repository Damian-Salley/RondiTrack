using Microsoft.AspNetCore.Mvc;
using RondiTrack.DTOs.Users;
using RondiTrack.Mappers;
using RondiTrack.Repositories;
using RondiTrack.Services;
using RondiTrack.Exceptions;

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

    // Get all users
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetUsers()
    {
        var users = await _repository.GetUsersAsync();

        var responses = new List<UserResponse>();

        foreach (var user in users)
        {
            responses.Add(UserMapper.ToResponse(user));
        }

        return Ok(responses);
    }

    // Get a user by ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUser(int id)
    {
        var user = await _repository.GetUserByIdAsync(id);

        if (user is null)
        {
            throw new NotFoundException(
                $"No user with ID {id} was found.");
        }

        return Ok(UserMapper.ToResponse(user));
    }

    // Create a new user
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(
        CreateUserRequest request)
    {
        var existingUser = await _repository.GetUserByIdAsync(request.Id);

        if (existingUser is not null)
        {
            if (existingUser is not null)
            {
                throw new BusinessRuleException(
                    $"A user with ID {request.Id} already exists.");
            }
        }


        var user = UserMapper.ToDomain(request);

        await _repository.AddUserAsync(user);

        var response = UserMapper.ToResponse(user);

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            response);
    }

    // Update an existing user
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        int id,
        UpdateUserRequest request)
    {
        var existingUser = await _repository.GetUserByIdAsync(id);

        if (existingUser is null)
        {
            throw new NotFoundException(
                $"No user with ID {id} was found.");
        }

        UserMapper.Update(existingUser, request);

        await _repository.UpdateUserAsync(existingUser);

        return Ok(UserMapper.ToResponse(existingUser));
    }

    // Delete a user by ID
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var deleted = await _repository.DeleteUserAsync(id);

        if (!deleted)
        {
            throw new NotFoundException(
                $"No user with ID {id} was found.");
        }

        return NoContent();
    }
}