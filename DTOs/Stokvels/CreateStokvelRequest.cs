namespace RondiTrack.DTOs.Stokvels;

// This class represents a request to create a new Stokvel.
public class CreateStokvelRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ContributionAmount { get; set; }
}