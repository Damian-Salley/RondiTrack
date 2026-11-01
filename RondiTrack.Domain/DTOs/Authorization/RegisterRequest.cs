namespace RondiTrack.Domain.DTOs.Authorization;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password
);