using RondiTrack.DTOs.ContributionCycles;
using RondiTrack.Models;

namespace RondiTrack.Mappers;

// Converts contribution cycles between API requests and domain models.
public static class ContributionCycleMapper
{
    // Creates a new cycle using the application clock and fixed member contribution.
    public static ContributionCycle ToDomain(
        CreateContributionCycleRequest request,
        TimeProvider timeProvider,
        decimal requiredContributionAmount)
    {
        // Calculate the end of the current month in Johannesburg time.
        var endsAtUtc = ContributionCycle.GetMonthEndUtc(
            timeProvider.GetUtcNow());

        // Create the cycle with its own stored per-member contribution amount.
        return new ContributionCycle(
            request.Id,
            request.StokvelId,
            request.Number,
            request.TargetAmount,
            endsAtUtc,
            requiredContributionAmount);
    }

    // Converts a contribution cycle into the API response format.
    public static ContributionCycleResponse ToResponse(
        ContributionCycle cycle)
    {
        return new ContributionCycleResponse
        {
            Id = cycle.Id,
            StokvelId = cycle.StokvelId,
            Number = cycle.Number,
            TargetAmount = cycle.TargetAmount
        };
    }

    // Updates the cycle's number and total collection target.
    // The historical per-member contribution amount remains unchanged.
    public static void Update(
        ContributionCycle cycle,
        UpdateContributionCycleRequest request)
    {
        cycle.Update(
            request.Number,
            request.TargetAmount);
    }
}