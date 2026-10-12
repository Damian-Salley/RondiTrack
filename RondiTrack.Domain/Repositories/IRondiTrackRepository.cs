using RondiTrack.Models;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.Pagination;

namespace RondiTrack.Repositories;

// EfRondiTrackRepository implements these methods using EF Core.
public interface IRondiTrackRepository
{
    // =========================================================
    // USER OPERATIONS
    // =========================================================

    // Gets all users from the database.
    Task<IReadOnlyCollection<User>> GetUsersAsync();

    // Gets one user using their ID.
    Task<User?> GetUserByIdAsync(int id);

    // Gets one user using their email
    Task<User?> GetUserByEmailAsync(string email);

    // Adds a new user to the database.
    Task AddUserAsync(User user);

    // Updates an existing user.
    // Returns true if the update succeeds.
    Task<bool> UpdateUserAsync(User user);

    // Deletes a user using their ID.
    // Returns true if the user was found and deleted.
    Task<bool> DeleteUserAsync(int id);


    // =========================================================
    // CONTRIBUTION CYCLE OPERATIONS
    // =========================================================

    // Gets all contribution cycles.
    Task<IReadOnlyCollection<ContributionCycle>> GetContributionCyclesAsync();

    // Gets one contribution cycle using its ID.
    Task<ContributionCycle?> GetContributionCycleByIdAsync(int id);

    // Adds a new contribution cycle.
    Task AddContributionCycleAsync(ContributionCycle cycle);

    // Updates an existing contribution cycle.
    Task UpdateContributionCycleAsync(ContributionCycle cycle);

    // Deletes a contribution cycle using its ID.
    Task<bool> DeleteContributionCycleAsync(int id);


    // =========================================================
    // STOKVEL OPERATIONS
    // =========================================================

    // Gets all stokvels.
    Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync();

    // Gets one stokvel using its ID.
    Task<Stokvel?> GetStokvelByIdAsync(int id);

    // Adds a new stokvel.
    Task AddStokvelAsync(Stokvel stokvel);

    // Updates an existing stokvel.
    Task<bool> UpdateStokvelAsync(Stokvel stokvel);

    // Deletes a stokvel using its ID.
    Task<bool> DeleteStokvelAsync(int id);


    // =========================================================
    // CONTRIBUTION OPERATIONS
    // =========================================================

    // Adds a contribution to the database.
    Task AddContributionAsync(Contribution contribution);

    // Gets one specific contribution.
    Task<Contribution?> GetContributionAsync(
        int stokvelId,
        int userId,
        int cycle);

    // Gets all contributions for a specific stokvel and contribution cycle.
    Task<IReadOnlyCollection<Contribution>> GetContributionsByCycleAsync(
        int stokvelId,
        int cycleId);

    // Gets contribution response data for a specific cycle.
    Task<IReadOnlyCollection<ContributionResponse>> GetContributionResponsesByCycleAsync(
        int stokvelId,
        int cycleId);

    // Gets filtered, sorted and paged contribution response data.
    Task<PagedResponse<ContributionResponse>> GetPagedContributionResponsesByCycleAsync(
        int stokvelId,
        int cycleId,
        ContributionPageRequest request);

    // Gets the member and user for a specific contribution.
    Task<StokvelMember?> GetMemberForContributionAsync(
        int stokvelId,
        int userId);


    // =========================================================
    // PAYOUT OPERATIONS
    // =========================================================

    // Adds a payout to the database.
    Task AddPayoutAsync(Payout payout);

    // Gets all payouts.
    Task<IReadOnlyCollection<Payout>> GetPayoutsAsync();

    // Processes a payout and updates the contribution cycle.
    Task ProcessPayoutAsync(
        Payout payout,
        ContributionCycle cycle);

    // Save a refresh token to the database.
    Task AddRefreshTokenAsync(RefreshToken token);

    // Find a refresh token using its hash.
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);

    // Retrieve all tokens belonging to one token family.
    Task<List<RefreshToken>> GetRefreshTokensByFamilyAsync(Guid familyId);

    // Save changes to existing refresh tokens.
    Task SaveChangesAsync();

    // Atomically mark an unused refresh token as used.
    Task<bool> TryUseRefreshTokenAsync(string tokenHash);

    // Atomically rotate a refresh token and save its replacement.
    Task<bool> RotateRefreshTokenAsync(
        string tokenHash,
        RefreshToken replacementToken);
}