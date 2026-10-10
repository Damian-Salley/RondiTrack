namespace RondiTrack.Models;

public class StokvelMember
{
    public int StokvelId { get; private set; }
    public int UserId { get; private set; }

    public Stokvel Stokvel { get; private set; } = null!;
    public User User { get; private set; } = null!;

    public string Role { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    public StokvelMember(
        int stokvelId,
        int userId,
        string role,
        DateTime joinedAtUtc)
    {
        StokvelId = stokvelId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }
}