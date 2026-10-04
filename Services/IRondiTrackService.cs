using RondiTrack.Models;
using RondiTrack.DTOs.Contributions;

namespace RondiTrack.Services;

public interface IRondiTrackService
{
    //Member operations
    Task AddMemberAsync(int stokvelId, int userId);
    Task RemoveMemberAsync(int stokvelId, int userId);

    //Contribution operations
    Task<Contribution> RecordContributionAsync(
        int stokvelId,
        int userId,
        int cycle,
        string idempotencyKey);

    //Gets all contributions for a specific stokvel and contribution cycle
    Task<IReadOnlyCollection<Contribution>> GetContributionsByCycleAsync(
        int stokvelId,
        int cycleId);

    Task<IReadOnlyCollection<ContributionResponse>> GetContributionResponsesByCycleAsync(
        int stokvelId,
        int cycleId);

    //Payout operations
    Task<Payout> ProcessPayoutAsync(
        int stokvelId,
        int contributionCycleId);
}