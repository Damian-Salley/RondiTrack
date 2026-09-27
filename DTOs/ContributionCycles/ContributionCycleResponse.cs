namespace RondiTrack.DTOs.ContributionCycles;

public class ContributionCycleResponse
{
    public int Id { get; set; }
    public int StokvelId { get; set; }
    public int Number { get; set; }
    public decimal TargetAmount { get; set; }
}