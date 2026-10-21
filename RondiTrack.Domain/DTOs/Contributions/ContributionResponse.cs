namespace RondiTrack.DTOs.Contributions;

using System.Text.Json.Serialization;

// Represents contribution information returned by the API.
public class ContributionResponse
{
    // Existing contribution identifiers.
    public int UserId { get; set; }
    public int StokvelId { get; set; }
    public int Cycle { get; set; }

    // Human-readable cycle number displayed in the frontend.
    // This differs from Cycle, which stores the database cycle ID.
    public int CycleNumber { get; set; }

    // Used internally for pagination; excluded from API responses.
    [JsonIgnore]
    public decimal ContributionAmount { get; set; }

    // Exact monetary amount for the frontend, expressed in cents.
    public long AmountCents { get; set; }

    // Contribution status: Pending, Paid, or Confirmed.
    public string Status { get; set; } = string.Empty;

    // UTC timestamp of the payment, if known.
    public DateTime? PaidAtUtc { get; set; }

    // Display name of the contributing member.
    public string MemberName { get; set; } = string.Empty;

    // Stable identifier derived from the existing composite key.
    // No database primary-key changes are required.
    public string Id => $"{StokvelId}-{UserId}-{Cycle}";
}