using RondiTrack.DTOs.Contributions;
using RondiTrack.Models;

namespace RondiTrack.Mappers;

public static class ContributionMapper
{
    public static ContributionResponse ToResponse(Contribution contribution)
    {
        return new ContributionResponse
        {
            UserId = contribution.UserId,
            StokvelId = contribution.StokvelId,
            Cycle = contribution.Cycle,
            ContributionAmount = contribution.ContributionAmount
        };
    }
}