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
    [EndpointSummary("Get all users")]
    [EndpointDescription("Returns all users currently stored in RondiTrack.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), StatusCodes.Status200OK)]
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
    [EndpointSummary("Get a user by ID")]
    [EndpointDescription("Returns the user with the specified ID.")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Create a user")]
    [EndpointDescription("Creates a new user. The request must be valid and the user ID must not already exist.")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        CreateUserRequest request)
    {
        var existingUser = await _repository.GetUserByIdAsync(request.Id);

        if (existingUser is not null)
        {
            throw new BusinessRuleException(
                $"A user with ID {request.Id} already exists.");
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
    [EndpointSummary("Update a user")]
    [EndpointDescription("Updates an existing user's details.")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Delete a user")]
    [EndpointDescription("Deletes the user with the specified ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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