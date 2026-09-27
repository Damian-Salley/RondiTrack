using RondiTrack.DTOs.ContributionCycles;
using RondiTrack.Models;

namespace RondiTrack.Mappers;

public static class ContributionCycleMapper
{
    public static ContributionCycle ToDomain(
        CreateContributionCycleRequest request)
    {
        return new ContributionCycle(
            request.Id,
            request.StokvelId,
            request.Number,
            request.TargetAmount);
    }

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

    public static void Update(
        ContributionCycle cycle,
        UpdateContributionCycleRequest request)
    {
        cycle.Update(
            request.Number,
            request.TargetAmount);
    }
}