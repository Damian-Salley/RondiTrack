namespace RondiTrack.DTOs.ContributionCycles;

public class ContributionCycleResponse
{
    public int Id { get; set; }

    public int StokvelId { get; set; }

    public int Number { get; set; }

    // Keep the original decimal amount for existing API clients.
    public decimal TargetAmount { get; set; }

    // Expose exact integer-cent amounts for mobile clients.
    public long TargetAmountCents { get; set; }

    public long RequiredContributionCents { get; set; }

    // Expose the cycle's status and UTC due date.
    public string Status { get; set; } = string.Empty;

    public DateTime DueDateUtc { get; set; }
}