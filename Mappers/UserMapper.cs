using RondiTrack.DTOs.Users;
using RondiTrack.Models;

namespace RondiTrack.Mappers;

public static class UserMapper
{
    // Maps a User model to a UserResponse DTO.
    public static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }

    // Maps a CreateUserRequest DTO to a User model.
    public static User ToDomain(CreateUserRequest request)
    {
        return new User(

            request.Id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber
        );
    }

    // Updates a User model with data from an UpdateUserRequest DTO.
    public static void Update(User user, UpdateUserRequest request)
    {
        user.UpdateDetails(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber 
        );
    }
}