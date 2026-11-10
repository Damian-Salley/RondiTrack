using RondiTrack.Models;

namespace RondiTrack.DTOs.Stokvels;

public class StokvelResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Keep the original decimal amount for existing API clients.
    public decimal ContributionAmount { get; set; }

    // Also expose the amount as exact integer cents for mobile clients.
    public long ContributionAmountCents =>
        ContributionLimits.ToCents(ContributionAmount);

    public int MemberCount { get; set; }
}