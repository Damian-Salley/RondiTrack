namespace RondiTrack.DTOs.Stokvels;

public class UpdateStokvelRequest
{
    public Guid? Version { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal ContributionAmount { get; set; }
}