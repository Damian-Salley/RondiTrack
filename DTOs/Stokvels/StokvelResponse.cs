namespace RondiTrack.DTOs.Stokvels;

public class StokvelResponse
{
    // Properties for StokvelResponse
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ContributionAmount { get; set; }
    public int MemberCount { get; set; }
}