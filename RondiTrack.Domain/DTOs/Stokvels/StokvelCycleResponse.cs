namespace RondiTrack.DTOs.Stokvels;

// Data required to display a contribution cycle.
public class StokvelCycleResponse
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDateUtc { get; set; }
    public long TotalCollectedCents { get; set; }
    public long AmountDueCents { get; set; }
}