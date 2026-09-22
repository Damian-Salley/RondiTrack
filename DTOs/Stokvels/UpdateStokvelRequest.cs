namespace RondiTrack.DTOs.Stokvels;

public class UpdateStokvelRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal ContributionAmount { get; set; }
}