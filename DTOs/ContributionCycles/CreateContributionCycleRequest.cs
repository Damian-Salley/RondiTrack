namespace RondiTrack.DTOs.ContributionCycles;

public class CreateContributionCycleRequest
{
    public int Id { get; set; }
    public int StokvelId { get; set; }
    public int Number { get; set; }
    public decimal TargetAmount { get; set; }
}