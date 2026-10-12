namespace RondiTrack.Models;

public class RefreshToken
{
    // Primary key.
    public int Id { get; set; }

    // User who owns the token.
    public int UserId { get; set; }

    // SHA-256 hash of the refresh token.
    public string TokenHash { get; set; } = string.Empty;

    // Groups rotated tokens into one session.
    public Guid FamilyId { get; set; }

    // Token expiration time.
    public DateTime ExpiresAt { get; set; }

    // Indicates whether the token has been revoked.
    public bool IsRevoked { get; set; }

    // Indicates whether the token has already been used.
    public bool IsUsed { get; set; }
}