using RondiTrack.Models;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.Pagination;

namespace RondiTrack.Repositories;

public class InMemoryRondiTrackRepository : IRondiTrackRepository
{
    private readonly List<Contribution> _contributions = new();
    private readonly List<ContributionCycle> _contributionCycles = new();
    private readonly List<Payout> _payouts = new();
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


    // =========================================================
    // USER OPERATIONS
    // =========================================================

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
        var index =
            _users.FindIndex(existingUser =>
                existingUser.Id == user.Id);

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
        var user =
            _users.FirstOrDefault(user =>
                user.Id == id);

        if (user is null)
        {
            return Task.FromResult(false);
        }

        _users.Remove(user);

        return Task.FromResult(true);
    }


    // =========================================================
    // CONTRIBUTION CYCLE OPERATIONS
    // =========================================================

    //Get all contribution cycles
    public Task<IReadOnlyCollection<ContributionCycle>>
        GetContributionCyclesAsync()
    {
        IReadOnlyCollection<ContributionCycle> cycles =
            _contributionCycles.ToList();

        return Task.FromResult(cycles);
    }

    //Get contribution cycle by ID
    public Task<ContributionCycle?> GetContributionCycleByIdAsync(int id)
    {
        var cycle =
            _contributionCycles.FirstOrDefault(cycle =>
                cycle.Id == id);

        return Task.FromResult(cycle);
    }

    //Add a contribution cycle
    public Task AddContributionCycleAsync(ContributionCycle cycle)
    {
        _contributionCycles.Add(cycle);

        return Task.CompletedTask;
    }

    //Update a contribution cycle
    public Task UpdateContributionCycleAsync(ContributionCycle cycle)
    {
        return Task.CompletedTask;
    }

    //Delete a contribution cycle
    public Task<bool> DeleteContributionCycleAsync(int id)
    {
        var cycle =
            _contributionCycles.FirstOrDefault(cycle =>
                cycle.Id == id);

        if (cycle is null)
        {
            return Task.FromResult(false);
        }

        _contributionCycles.Remove(cycle);

        return Task.FromResult(true);
    }


    // =========================================================
    // STOKVEL OPERATIONS
    // =========================================================

    //Get stokvel by ID
    public Task<Stokvel?> GetStokvelByIdAsync(int id)
    {
        var stokvel =
            _stokvels.FirstOrDefault(stokvel =>
                stokvel.Id == id);

        return Task.FromResult(stokvel);
    }

    //Get all stokvels
    public Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync()
    {
        IReadOnlyCollection<Stokvel> stokvels =
            _stokvels.AsReadOnly();

        return Task.FromResult(stokvels);
    }

    //Add a new stokvel
    public Task AddStokvelAsync(Stokvel stokvel)
    {
        _stokvels.Add(stokvel);

        return Task.CompletedTask;
    }

    //Update an existing stokvel
    public Task<bool> UpdateStokvelAsync(Stokvel stokvel)
    {
        var index =
            _stokvels.FindIndex(existingStokvel =>
                existingStokvel.Id == stokvel.Id);

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
        var stokvel =
            _stokvels.FirstOrDefault(stokvel =>
                stokvel.Id == id);

        if (stokvel is null)
        {
            return Task.FromResult(false);
        }

        _stokvels.Remove(stokvel);

        return Task.FromResult(true);
    }


    // =========================================================
    // CONTRIBUTION OPERATIONS
    // =========================================================

    //Add a contribution
    public Task AddContributionAsync(Contribution contribution)
    {
        _contributions.Add(contribution);

        return Task.CompletedTask;
    }

    //Get a contribution by stokvel ID, user ID, and cycle
    public Task<Contribution?> GetContributionAsync(
        int stokvelId,
        int userId,
        int cycle)
    {
        var contribution =
            _contributions.FirstOrDefault(contribution =>
                contribution.StokvelId == stokvelId &&
                contribution.UserId == userId &&
                contribution.Cycle == cycle);

        return Task.FromResult(contribution);
    }

    //Get all contributions for a stokvel and contribution cycle
    public Task<IReadOnlyCollection<Contribution>>
        GetContributionsByCycleAsync(
            int stokvelId,
            int cycleId)
    {
        IReadOnlyCollection<Contribution> contributions =
            _contributions
                .Where(contribution =>
                    contribution.StokvelId == stokvelId &&
                    contribution.Cycle == cycleId)
                .ToList();

        return Task.FromResult(contributions);
    }

    //Gets contribution response data using projection
    public Task<IReadOnlyCollection<ContributionResponse>>
        GetContributionResponsesByCycleAsync(
            int stokvelId,
            int cycleId)
    {
        var responses =
            _contributions
                .Where(contribution =>
                    contribution.StokvelId == stokvelId &&
                    contribution.Cycle == cycleId)
                .Select(contribution =>
                {
                    var user =
                        _users.First(user =>
                            user.Id == contribution.UserId);

                    return new ContributionResponse
                    {
                        UserId = contribution.UserId,
                        StokvelId = contribution.StokvelId,
                        Cycle = contribution.Cycle,
                        ContributionAmount =
                            contribution.ContributionAmount,
                        MemberName =
                            $"{user.FirstName} {user.LastName}"
                    };
                })
                .ToList();

        return Task.FromResult<
            IReadOnlyCollection<ContributionResponse>>(
                responses);
    }

    //Gets filtered, sorted and paged contribution response data
    public Task<PagedResponse<ContributionResponse>>
        GetPagedContributionResponsesByCycleAsync(
            int stokvelId,
            int cycleId,
            ContributionPageRequest request)
    {
        const int defaultPageSize = 20;
        const int maximumPageSize = 100;

        if (request.PageSize < 0)
        {
            throw new ArgumentException(
                "Page size cannot be negative.");
        }

        var pageSize =
            request.PageSize is null or 0
                ? defaultPageSize
                : Math.Min(
                    request.PageSize.Value,
                    maximumPageSize);

        var sortBy =
            request.SortBy.Trim().ToLowerInvariant();

        if (sortBy != "userid" &&
            sortBy != "amount")
        {
            throw new ArgumentException(
                "SortBy must be 'userId' or 'amount'.");
        }

        PageToken? token = null;

        if (!string.IsNullOrWhiteSpace(request.PageToken))
        {
            token =
                PageToken.Decode(request.PageToken);

            if (token is null)
            {
                throw new ArgumentException(
                    "The page token is invalid.");
            }

            var expectedFilter =
                request.UserId?.ToString();

            if (token.SortBy != sortBy ||
                token.Filter != expectedFilter)
            {
                throw new ArgumentException(
                    "The page token cannot be used with different filter or sort values.");
            }
        }

        var query =
            _contributions
                .Where(contribution =>
                    contribution.StokvelId == stokvelId &&
                    contribution.Cycle == cycleId);

        if (request.UserId.HasValue)
        {
            query =
                query.Where(contribution =>
                    contribution.UserId ==
                    request.UserId.Value);
        }

        if (sortBy == "userid")
        {
            query =
                query.OrderBy(contribution =>
                    contribution.UserId);

            if (token is not null)
            {
                query =
                    query.Where(contribution =>
                        contribution.UserId >
                        token.LastId);
            }
        }
        else
        {
            query =
                query
                    .OrderBy(contribution =>
                        contribution.ContributionAmount)
                    .ThenBy(contribution =>
                        contribution.UserId);

            if (token is not null &&
                token.LastAmount.HasValue)
            {
                var lastAmount =
                    token.LastAmount.Value;

                var lastId =
                    token.LastId;

                query =
                    query.Where(contribution =>
                        contribution.ContributionAmount >
                            lastAmount ||
                        (contribution.ContributionAmount ==
                            lastAmount &&
                         contribution.UserId > lastId));
            }
        }

        var responses =
            query
                .Take(pageSize + 1)
                .Select(contribution =>
                {
                    var user =
                        _users.First(user =>
                            user.Id == contribution.UserId);

                    return new ContributionResponse
                    {
                        UserId = contribution.UserId,
                        StokvelId = contribution.StokvelId,
                        Cycle = contribution.Cycle,
                        ContributionAmount =
                            contribution.ContributionAmount,
                        MemberName =
                            $"{user.FirstName} {user.LastName}"
                    };
                })
                .ToList();

        var hasMore =
            responses.Count > pageSize;

        var items =
            responses.Take(pageSize).ToList();

        string? nextPageToken = null;

        if (hasMore)
        {
            var lastItem =
                items[^1];

            nextPageToken =
                new PageToken
                {
                    LastId = lastItem.UserId,
                    LastAmount =
                        sortBy == "amount"
                            ? lastItem.ContributionAmount
                            : null,
                    SortBy = sortBy,
                    Filter = request.UserId?.ToString()
                }
                .Encode();
        }

        return Task.FromResult(
            new PagedResponse<ContributionResponse>
            {
                Items = items,
                NextPageToken = nextPageToken
            });
    }

    //Gets the member and user for a specific contribution
    public Task<StokvelMember?> GetMemberForContributionAsync(
        int stokvelId,
        int userId)
    {
        var stokvel =
            _stokvels.FirstOrDefault(stokvel =>
                stokvel.Id == stokvelId);

        var member =
            stokvel?.Members.FirstOrDefault(member =>
                member.UserId == userId);

        return Task.FromResult(member);
    }


    // =========================================================
    // PAYOUT OPERATIONS
    // =========================================================

    //Add a payout
    public Task AddPayoutAsync(Payout payout)
    {
        _payouts.Add(payout);

        return Task.CompletedTask;
    }

    //Process a payout
    public Task ProcessPayoutAsync(
        Payout payout,
        ContributionCycle cycle)
    {
        cycle.MarkAsPaid();
        _payouts.Add(payout);

        return Task.CompletedTask;
    }

    //Get all payouts
    public Task<IReadOnlyCollection<Payout>> GetPayoutsAsync()
    {
        return Task.FromResult<
            IReadOnlyCollection<Payout>>(
                _payouts);
    }
}