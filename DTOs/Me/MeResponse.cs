namespace RondiTrack.DTOs.Me;

public record MeResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email
);