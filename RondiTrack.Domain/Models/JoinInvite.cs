namespace RondiTrack.Models;

public class JoinInvite
{
    public string Code { get; set; } = "";
    public int StokvelId { get; set; }
    public string Rules { get; set; } = "";
    public string Schedule { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
}
