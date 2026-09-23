using RondiTrack.Models;

namespace RondiTrack.Services;

public interface IRondiTrackService
{
    Task AddMemberAsync(int stokvelId, int userId);
    Task RemoveMemberAsync(int stokvelId, int userId);

    Task<Contribution> RecordContributionAsync(
        int stokvelId,
        int userId,
        int cycle,
        string idempotencyKey);
}