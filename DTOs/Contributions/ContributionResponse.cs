namespace RondiTrack.DTOs.Contributions;

public class ContributionResponse
{
    public int UserId { get; set; }
    public int StokvelId { get; set; }
    public int Cycle { get; set; }
    public decimal ContributionAmount { get; set; }
}