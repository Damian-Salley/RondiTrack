using Microsoft.EntityFrameworkCore;
using RondiTrack.Data;
using RondiTrack.Models;

namespace RondiTrack.Repositories;

public class EfRondiTrackRepository : IRondiTrackRepository
{
    private readonly RondiTrackDbContext _context;

    public EfRondiTrackRepository(RondiTrackDbContext context)
    {
        _context = context;
    }

    // Users

    public async Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        var existingUser = await _context.Users.FindAsync(user.Id);

        if (existingUser is null)
        {
            return false;
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user is null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    // Contribution Cycles

    public async Task<IReadOnlyCollection<ContributionCycle>> GetContributionCyclesAsync()
    {
        return await _context.ContributionCycles.ToListAsync();
    }

    public async Task<ContributionCycle?> GetContributionCycleByIdAsync(int id)
    {
        return await _context.ContributionCycles.FindAsync(id);
    }

    public async Task AddContributionCycleAsync(ContributionCycle cycle)
    {
        await _context.ContributionCycles.AddAsync(cycle);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateContributionCycleAsync(ContributionCycle cycle)
    {
        _context.ContributionCycles.Update(cycle);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteContributionCycleAsync(int id)
    {
        var cycle = await _context.ContributionCycles.FindAsync(id);

        if (cycle is null)
        {
            return false;
        }

        _context.ContributionCycles.Remove(cycle);
        await _context.SaveChangesAsync();

        return true;
    }

    // Stokvels

    public async Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync()
    {
        return await _context.Stokvels.ToListAsync();
    }

    public async Task<Stokvel?> GetStokvelByIdAsync(int id)
    {
        return await _context.Stokvels
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddStokvelAsync(Stokvel stokvel)
    {
        await _context.Stokvels.AddAsync(stokvel);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateStokvelAsync(Stokvel stokvel)
    {
        var existingStokvel = await _context.Stokvels.FindAsync(stokvel.Id);

        if (existingStokvel is null)
        {
            return false;
        }

        _context.Stokvels.Update(stokvel);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteStokvelAsync(int id)
    {
        var stokvel = await _context.Stokvels.FindAsync(id);

        if (stokvel is null)
        {
            return false;
        }

        _context.Stokvels.Remove(stokvel);
        await _context.SaveChangesAsync();

        return true;
    }

    // Contributions

    public async Task AddContributionAsync(Contribution contribution)
    {
        await _context.Contributions.AddAsync(contribution);
        await _context.SaveChangesAsync();
    }

    public async Task<Contribution?> GetContributionAsync(
        int stokvelId,
        int userId,
        int cycle)
    {
        return await _context.Contributions.FirstOrDefaultAsync(c =>
            c.StokvelId == stokvelId &&
            c.UserId == userId &&
            c.Cycle == cycle);
    }

    public async Task AddPayoutAsync(Payout payout)
    {
        await _context.Payouts.AddAsync(payout);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<Payout>> GetPayoutsAsync()
    {
        return await _context.Payouts.ToListAsync();
    }

    public async Task ProcessPayoutAsync(
    Payout payout,
    ContributionCycle cycle)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            cycle.MarkAsPaid();

            _context.ContributionCycles.Update(cycle);
            await _context.SaveChangesAsync();

            await _context.Payouts.AddAsync(payout);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    
}