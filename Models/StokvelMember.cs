namespace RondiTrack.Models;

public class StokvelMember
{
    public int StokvelId { get; private set; }
    public int UserId { get; private set; }

    public StokvelMember(int stokvelId, int userId)
    {
        StokvelId = stokvelId;
        UserId = userId;
    }
}