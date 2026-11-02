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
    // Stores refresh tokens in memory.
    private readonly List<RefreshToken> _refreshTokens = new();


    public Task<User?> GetUserByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        var user = _users.FirstOrDefault(user =>
            user.NormalizedEmail == normalizedEmail);

        return Task.FromResult(user);
    }


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

    // The in-memory repository has no arrears storage.
    // Return zero because no arrears records exist here.
    public Task<long> GetArrearsCentsAsync(int stokvelId, int userId)
    {
        return Task.FromResult(0L);
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
                        // Find the display number of the associated contribution cycle.
                        CycleNumber = _contributionCycles
    .First(cycle => cycle.Id == contribution.Cycle)
    .Number,
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
                        // Find the display number of the associated contribution cycle.
                        CycleNumber = _contributionCycles
    .First(cycle => cycle.Id == contribution.Cycle)
    .Number,
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

    // Returns recent payments from stokvels the authenticated user belongs to.
    public Task<PagedResponse<ContributionResponse>> GetActivityAsync(
        int userId,
        int pageSize,
        string? pageToken)
    {
        // Reject invalid user IDs and page sizes.
        if (userId <= 0 || pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Invalid user ID or page size.");

        ActivityPageToken? token = null;

        if (!string.IsNullOrWhiteSpace(pageToken))
        {
            token = ActivityPageToken.Decode(pageToken);

            // The cursor must be valid and belong to this user.
            if (token is null ||
                token.UserId != userId ||
                token.LastPaidAtUtc.Kind != DateTimeKind.Utc ||
                token.LastStokvelId <= 0 ||
                token.LastCycle <= 0 ||
                token.LastUserId <= 0)
            {
                throw new ArgumentException("Invalid activity page token.");
            }
        }

        // Find the stokvels the authenticated user belongs to.
        var stokvelIds = _stokvels
            .Where(stokvel => stokvel.Members.Any(member =>
                member.UserId == userId))
            .Select(stokvel => stokvel.Id)
            .ToHashSet();

        // Include only paid contributions from those stokvels.
        var query = _contributions.Where(contribution =>
            stokvelIds.Contains(contribution.StokvelId) &&
            contribution.PaidAtUtc.HasValue);

        if (token is not null)
        {
            // Continue after the previous page's last contribution.
            query = query.Where(contribution =>
                contribution.PaidAtUtc < token.LastPaidAtUtc ||
                (contribution.PaidAtUtc == token.LastPaidAtUtc &&
                 contribution.StokvelId < token.LastStokvelId) ||
                (contribution.PaidAtUtc == token.LastPaidAtUtc &&
                 contribution.StokvelId == token.LastStokvelId &&
                 contribution.Cycle < token.LastCycle) ||
                (contribution.PaidAtUtc == token.LastPaidAtUtc &&
                 contribution.StokvelId == token.LastStokvelId &&
                 contribution.Cycle == token.LastCycle &&
                 contribution.UserId < token.LastUserId));
        }

        // Use the same ordering as the PostgreSQL implementation.
        var results = query
            .OrderByDescending(contribution => contribution.PaidAtUtc)
            .ThenByDescending(contribution => contribution.StokvelId)
            .ThenByDescending(contribution => contribution.Cycle)
            .ThenByDescending(contribution => contribution.UserId)
            .Take(pageSize + 1)
            .Select(contribution =>
            {
                var user = _users.First(user =>
                    user.Id == contribution.UserId);

                var cycle = _contributionCycles.FirstOrDefault(cycle =>
                    cycle.Id == contribution.Cycle);

                return new ContributionResponse
                {
                    UserId = contribution.UserId,
                    StokvelId = contribution.StokvelId,
                    Cycle = contribution.Cycle,
                    CycleNumber = cycle?.Number ?? 0,
                    ContributionAmount = contribution.ContributionAmount,
                    AmountCents = (long)(contribution.ContributionAmount * 100m),
                    Status = contribution.Status,
                    PaidAtUtc = contribution.PaidAtUtc,
                    MemberName = $"{user.FirstName} {user.LastName}"
                };
            })
            .ToList();

        // The extra record tells us whether another page exists.
        var hasMore = results.Count > pageSize;
        var items = results.Take(pageSize).ToList();

        string? nextPageToken = null;

        if (hasMore)
        {
            var last = items[^1];

            nextPageToken = new ActivityPageToken
            {
                LastPaidAtUtc = last.PaidAtUtc!.Value,
                LastStokvelId = last.StokvelId,
                LastCycle = last.Cycle,
                LastUserId = last.UserId,
                UserId = userId
            }.Encode();
        }

        return Task.FromResult(new PagedResponse<ContributionResponse>
        {
            Items = items,
            NextPageToken = nextPageToken
        });
    }

    // Gets contribution history across all cycles,
    // with member filtering, UTC date filtering and pagination.
    public Task<PagedResponse<ContributionResponse>> GetContributionHistoryAsync(
        int stokvelId,
        ContributionPageRequest request)
    {
        const int defaultPageSize = 20;
        const int maximumPageSize = 100;

        if (request.PageSize < 0)
        {
            throw new ArgumentException("Page size cannot be negative.");
        }

        if (request.From.HasValue &&
            request.To.HasValue &&
            request.From >= request.To)
        {
            throw new ArgumentException("From must be earlier than To.");
        }

        var pageSize = request.PageSize is null or 0
            ? defaultPageSize
            : Math.Min(request.PageSize.Value, maximumPageSize);

        ContributionHistoryToken? token = null;

        if (!string.IsNullOrWhiteSpace(request.PageToken))
        {
            token = ContributionHistoryToken.Decode(request.PageToken);

            if (token is null ||
                token.StokvelId != stokvelId ||
                token.UserId != request.UserId ||
                token.From != request.From ||
                token.To != request.To ||
                token.LastCycle <= 0 ||
                token.LastUserId <= 0)
            {
                throw new ArgumentException(
                    "The page token is invalid or does not match the filters.");
            }
        }

        var query = _contributions
            .Where(contribution =>
                contribution.StokvelId == stokvelId);

        if (request.UserId.HasValue)
        {
            query = query.Where(contribution =>
                contribution.UserId == request.UserId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(contribution =>
                contribution.PaidAtUtc >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(contribution =>
                contribution.PaidAtUtc < request.To.Value);
        }

        if (token is not null)
        {
            query = query.Where(contribution =>
                contribution.PaidAtUtc < token.LastPaidAtUtc ||
                (contribution.PaidAtUtc == token.LastPaidAtUtc &&
                 contribution.Cycle < token.LastCycle) ||
                (contribution.PaidAtUtc == token.LastPaidAtUtc &&
                 contribution.Cycle == token.LastCycle &&
                 contribution.UserId < token.LastUserId));
        }

        var results = query
            .OrderByDescending(contribution => contribution.PaidAtUtc)
            .ThenByDescending(contribution => contribution.Cycle)
            .ThenByDescending(contribution => contribution.UserId)
            .Take(pageSize + 1)
            .Select(contribution =>
            {
                var user = _users.First(user =>
                    user.Id == contribution.UserId);

                var cycle = _contributionCycles.FirstOrDefault(cycle =>
                    cycle.Id == contribution.Cycle);

                return new ContributionResponse
                {
                    UserId = contribution.UserId,
                    StokvelId = contribution.StokvelId,
                    Cycle = contribution.Cycle,
                    CycleNumber = cycle?.Number ?? 0,
                    ContributionAmount = contribution.ContributionAmount,
                    AmountCents = (long)(contribution.ContributionAmount * 100m),
                    Status = contribution.Status,
                    PaidAtUtc = contribution.PaidAtUtc,
                    MemberName = $"{user.FirstName} {user.LastName}"
                };
            })
            .ToList();

        var hasMore = results.Count > pageSize;
        var items = results.Take(pageSize).ToList();

        string? nextPageToken = null;

        if (hasMore)
        {
            var lastItem = items[^1];

            nextPageToken = new ContributionHistoryToken
            {
                LastPaidAtUtc = lastItem.PaidAtUtc,
                LastCycle = lastItem.Cycle,
                LastUserId = lastItem.UserId,
                StokvelId = stokvelId,
                UserId = request.UserId,
                From = request.From,
                To = request.To
            }.Encode();
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

    // Save a new refresh token.
    public Task AddRefreshTokenAsync(RefreshToken token)
    {
        _refreshTokens.Add(token);
        return Task.CompletedTask;
    }

    // Find a refresh token by its hash.
    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
    {
        return Task.FromResult(
            _refreshTokens.FirstOrDefault(token => token.TokenHash == tokenHash));
    }

    // Retrieve all tokens belonging to the same family.
    public Task<List<RefreshToken>> GetRefreshTokensByFamilyAsync(Guid familyId)
    {
        return Task.FromResult(
            _refreshTokens.Where(token => token.FamilyId == familyId).ToList());
    }

    // Changes are already stored in memory, so nothing needs saving.
    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }

    // Consume a refresh token in the in-memory repository.
    public Task<bool> TryUseRefreshTokenAsync(string tokenHash)
    {
        // Find the token.
        var token = _refreshTokens.FirstOrDefault(
            token => token.TokenHash == tokenHash);

        // Reject invalid, expired or previously used tokens.
        if (token is null ||
            token.IsUsed ||
            token.IsRevoked ||
            token.ExpiresAt <= DateTime.UtcNow)
        {
            return Task.FromResult(false);
        }

        // Mark the token as used.
        token.IsUsed = true;

        return Task.FromResult(true);
    }

    // Rotate a refresh token in memory.
    public Task<bool> RotateRefreshTokenAsync(
        string tokenHash,
        RefreshToken replacementToken)
    {
        // Find the original token.
        var token = _refreshTokens.FirstOrDefault(
            token => token.TokenHash == tokenHash);

        // Reject invalid or previously consumed tokens.
        if (token is null ||
            token.IsUsed ||
            token.IsRevoked ||
            token.ExpiresAt <= DateTime.UtcNow)
        {
            return Task.FromResult(false);
        }

        // Consume the original token.
        token.IsUsed = true;

        // Store the replacement token.
        _refreshTokens.Add(replacementToken);

        return Task.FromResult(true);
    }
}