namespace RondiTrack.DTOs.Authorization;

public record LoginRequest(
    string Email,
    string Password
);