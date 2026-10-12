using System.Security.Cryptography;
using System.Text;

namespace RondiTrack.Security;

public static class RefreshTokenHelper
{
    // Generate a cryptographically secure random token.
    public static string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    // Hash a token using SHA-256 before database storage.
    public static string HashToken(string token)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hash);
    }
}