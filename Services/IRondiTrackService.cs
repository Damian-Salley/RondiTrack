using RondiTrack.Models;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.Pagination;

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

    //Gets filtered, sorted and paged contribution responses
    Task<PagedResponse<ContributionResponse>>
        GetPagedContributionResponsesByCycleAsync(
            int stokvelId,
            int cycleId,
            ContributionPageRequest request);

    //Payout operations
    Task<Payout> ProcessPayoutAsync(
        int stokvelId,
        int contributionCycleId);
}