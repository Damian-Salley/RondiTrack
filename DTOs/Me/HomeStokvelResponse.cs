namespace RondiTrack.DTOs.Me;

// Represents one stokvel on the authenticated user's home screen.
public class HomeStokvelResponse
{
    // Basic stokvel information.
    public int StokvelId { get; set; }
    public string StokvelName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Information about the active contribution cycle.
    public int? CurrentCycle { get; set; }
    public string? CycleStatus { get; set; }

    // Monetary amounts are strings to preserve decimal precision in JavaScript.
    public string PaidThisCycle { get; set; } = string.Empty;
    public string StillOwes { get; set; } = string.Empty;

    // The next scheduled payout date in UTC, if available.
    public DateTime? NextPayoutDate { get; set; }
}