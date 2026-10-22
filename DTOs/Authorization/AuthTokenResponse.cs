namespace RondiTrack.DTOs.Authorization;

public record AuthTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn
);