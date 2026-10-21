namespace RondiTrack.DTOs.Me;

// Represents one stokvel on the authenticated user's home screen.
public class HomeStokvelResponse
{
    // Basic stokvel information.
    public int StokvelId { get; set; }

    public string StokvelName { get; set; } = string.Empty;

    // The user's role within this specific stokvel.
    public string Role { get; set; } = string.Empty;

    // Information about the current contribution cycle.
    public int? CurrentCycle { get; set; }

    public string? CycleStatus { get; set; }

    // Monetary values are returned as integer cents.
    // For example, R500.00 is represented as 50000.
    public long PaidThisCycle { get; set; }

    public long StillOwes { get; set; }

    // The next scheduled payout date in UTC, if available.
    public DateTime? NextPayoutDate { get; set; }
}