namespace RondiTrack.DTOs.Authorization;

public record RegisterRequest(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password
);