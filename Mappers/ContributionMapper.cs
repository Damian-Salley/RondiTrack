using RondiTrack.DTOs.Contributions;
using RondiTrack.Models;

namespace RondiTrack.Mappers;

// Converts contribution domain models into API response objects.
public static class ContributionMapper
{
    public static ContributionResponse ToResponse(Contribution contribution)
    {
        return new ContributionResponse
        {
            UserId = contribution.UserId,
            StokvelId = contribution.StokvelId,
            Cycle = contribution.Cycle,

            // Retain the existing amount for pagination compatibility.
            ContributionAmount = contribution.ContributionAmount,

            // Convert the monetary amount into integer cents.
            AmountCents = checked(
                (long)(contribution.ContributionAmount * 100m)),

            // Return the actual stored payment information.
            Status = contribution.Status,
            PaidAtUtc = contribution.PaidAtUtc
        };
    }
}