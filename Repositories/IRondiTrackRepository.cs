using RondiTrack.Models;


namespace RondiTrack.Repositories;

public interface IRondiTrackRepository
{
    //CRUD Operations for User
    Task<IReadOnlyCollection<User>> GetUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task AddUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);
    Task<IReadOnlyCollection<ContributionCycle>> GetContributionCyclesAsync();
    Task<ContributionCycle?> GetContributionCycleByIdAsync(int id);
    Task AddContributionCycleAsync(ContributionCycle cycle);
    Task UpdateContributionCycleAsync(ContributionCycle cycle);
    Task<bool> DeleteContributionCycleAsync(int id);

    //CRUD Operations for Stokvel
    Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync();
    Task<Stokvel?> GetStokvelByIdAsync(int id);
    Task AddStokvelAsync(Stokvel stokvel);
    Task<bool> UpdateStokvelAsync(Stokvel stokvel);
    Task<bool> DeleteStokvelAsync(int id);
    Task AddContributionAsync(Contribution contribution);
    Task<Contribution?> GetContributionAsync(int stokvelId, int userId, int cycle);
    Task AddPayoutAsync(Payout payout);
    Task<IReadOnlyCollection<Payout>> GetPayoutsAsync();
    Task ProcessPayoutAsync(Payout payout, ContributionCycle cycle);
}