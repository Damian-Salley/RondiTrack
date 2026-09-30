using RondiTrack.Models;
using RondiTrack.DTOs.Users;

namespace RondiTrack.Repositories;

public class InMemoryRondiTrackRepository : IRondiTrackRepository

{
    private readonly List<Contribution> _contributions = new();
    private readonly List<ContributionCycle> _contributionCycles = new();
    private readonly List<Payout> _payouts = new();

    //Add a member to a stokvel
    public Task AddContributionAsync(Contribution contribution)
    {
        _contributions.Add(contribution);
        return Task.CompletedTask;
    }


    //CRUD Operations for User
    //Get all users
    public Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        IReadOnlyCollection<User> users = _users.AsReadOnly();

        return Task.FromResult(users);
    }

    //Get user by ID
    public Task<User?> GetUserByIdAsync(int id)
    {
        var user = _users.FirstOrDefault(user => user.Id == id);

        return Task.FromResult(user);
    }

    //Add a new user
    public Task AddUserAsync(User user)
    {
        _users.Add(user);

        return Task.CompletedTask;
    }

    //Update an existing user
    public Task<bool> UpdateUserAsync(User user)
    {
        var index = _users.FindIndex(existingUser => existingUser.Id == user.Id);

        if (index == -1)
        {
            return Task.FromResult(false);
        }

        _users[index] = user;

        return Task.FromResult(true);
    }

    //Delete a user by ID
    public Task<bool> DeleteUserAsync(int id)
    {
        var user = _users.FirstOrDefault(user => user.Id == id);

        if (user is null)
        {
            return Task.FromResult(false);
        }

        _users.Remove(user);

        return Task.FromResult(true);
    }

    private readonly List<User> _users;
    private readonly List<Stokvel> _stokvels;

    //Constructor to initialize the in-memory repository with some sample data
    public InMemoryRondiTrackRepository()
    {
        _users = new List<User>
        {
            new User(1, "Damian", "Salley", "damian@example.com", "0821234567"),
            new User(2, "Tim", "Huang", "tim@example.com", "0831234567"),
            new User(3, "Ryan", "Smith", "ryan@example.com", "0841234567")
        };

        var stokvel1 = new Stokvel(1, "Community Savings", 500m);

        stokvel1.AddMember(_users[0]);
        stokvel1.AddMember(_users[1]);
        stokvel1.AddMember(_users[2]);

        _stokvels = new List<Stokvel>
        {
            stokvel1
        };
    }

    //CRUD Operations for Stokvel
    //Get stokvel by ID
    public Task<Stokvel?> GetStokvelByIdAsync(int id)
    {
        var stokvel = _stokvels.FirstOrDefault(stokvel => stokvel.Id == id);

        return Task.FromResult(stokvel);
    }

    //Get all stokvels
    public Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync()
    {
        IReadOnlyCollection<Stokvel> stokvels = _stokvels.AsReadOnly();

        return Task.FromResult(stokvels);
    }

    public Task AddStokvelAsync(Stokvel stokvel)
    {
        _stokvels.Add(stokvel);

        return Task.CompletedTask;
    }

    //Update an existing stokvel
    public Task<bool> UpdateStokvelAsync(Stokvel stokvel)
    {
        var index = _stokvels.FindIndex(existingStokvel => existingStokvel.Id == stokvel.Id);

        if (index == -1)
        {
            return Task.FromResult(false);
        }

        _stokvels[index] = stokvel;

        return Task.FromResult(true);
    }

    //Delete a stokvel by ID
    public Task<bool> DeleteStokvelAsync(int id)
    {
        var stokvel = _stokvels.FirstOrDefault(stokvel => stokvel.Id == id);

        if (stokvel is null)
        {
            return Task.FromResult(false);
        }

        _stokvels.Remove(stokvel);

        return Task.FromResult(true);
    }

    //Get a contribution by stokvel ID, user ID, and cycle
    public Task<Contribution?> GetContributionAsync(
    int stokvelId,
    int userId,
    int cycle)
    {
        var contribution = _contributions.FirstOrDefault(c =>
            c.StokvelId == stokvelId &&
            c.UserId == userId &&
            c.Cycle == cycle);

        return Task.FromResult(contribution);
    }
    public Task<IReadOnlyCollection<ContributionCycle>> GetContributionCyclesAsync()
    {
        IReadOnlyCollection<ContributionCycle> cycles =
            _contributionCycles.ToList();

        return Task.FromResult(cycles);
    }

    public Task<ContributionCycle?> GetContributionCycleByIdAsync(int id)
    {
        var cycle = _contributionCycles
            .FirstOrDefault(c => c.Id == id);

        return Task.FromResult(cycle);
    }

    public Task AddContributionCycleAsync(ContributionCycle cycle)
    {
        _contributionCycles.Add(cycle);

        return Task.CompletedTask;
    }

    public Task UpdateContributionCycleAsync(ContributionCycle cycle)
    {
        return Task.CompletedTask;
    }

    public Task<bool> DeleteContributionCycleAsync(int id)
    {
        var cycle = _contributionCycles
            .FirstOrDefault(c => c.Id == id);

        if (cycle is null)
        {
            return Task.FromResult(false);
        }

        _contributionCycles.Remove(cycle);

        return Task.FromResult(true);
    }
    public Task AddPayoutAsync(Payout payout)
    {
        _payouts.Add(payout);
        return Task.CompletedTask;
    }

    public Task ProcessPayoutAsync(
    Payout payout,
    ContributionCycle cycle)
{
    cycle.MarkAsPaid();
    _payouts.Add(payout);

    return Task.CompletedTask;
}

    public Task<IReadOnlyCollection<Payout>> GetPayoutsAsync()
    {
        return Task.FromResult<IReadOnlyCollection<Payout>>(_payouts);
    }

}