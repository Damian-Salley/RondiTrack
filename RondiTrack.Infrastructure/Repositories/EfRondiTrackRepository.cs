using Microsoft.EntityFrameworkCore;
using RondiTrack.Data;
using RondiTrack.Models;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.Pagination;


namespace RondiTrack.Repositories;

public class EfRondiTrackRepository : IRondiTrackRepository
{
    private readonly RondiTrackDbContext _context;

    public EfRondiTrackRepository(RondiTrackDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // USER OPERATIONS
    // =========================================================

    // Gets all users from the database.
    public async Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync();
    }

    // Gets one user using their ID.
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return await _context.Users
            .FirstOrDefaultAsync(user =>
                user.NormalizedEmail == normalizedEmail);
    }

    // Adds a new user to the database.
    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    // Updates an existing user.
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

    // Deletes a user using their ID.
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


    // =========================================================
    // CONTRIBUTION CYCLE OPERATIONS
    // =========================================================

    // Gets all contribution cycles.
    public async Task<IReadOnlyCollection<ContributionCycle>> GetContributionCyclesAsync()
    {
        return await _context.ContributionCycles
            .AsNoTracking()
            .ToListAsync();
    }

    // Gets one contribution cycle using its ID.
    public async Task<ContributionCycle?> GetContributionCycleByIdAsync(int id)
    {
        return await _context.ContributionCycles.FindAsync(id);
    }

    // Adds a new contribution cycle.
    public async Task AddContributionCycleAsync(ContributionCycle cycle)
    {
        await _context.ContributionCycles.AddAsync(cycle);
        await _context.SaveChangesAsync();
    }

    // Updates an existing contribution cycle.
    public async Task UpdateContributionCycleAsync(ContributionCycle cycle)
    {
        _context.ContributionCycles.Update(cycle);
        await _context.SaveChangesAsync();
    }

    // Deletes a contribution cycle using its ID.
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


    // =========================================================
    // STOKVEL OPERATIONS
    // =========================================================

    // Gets all stokvels with their members.
    public async Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync()
    {
        return await _context.Stokvels
            .Include(stokvel => stokvel.Members)
            .AsNoTracking()
            .ToListAsync();
    }

    // Gets one stokvel using its ID.
    public async Task<Stokvel?> GetStokvelByIdAsync(int id)
    {
        return await _context.Stokvels
            .Include(stokvel => stokvel.Members)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(stokvel => stokvel.Id == id);
    }

    // Adds a new stokvel.
    public async Task AddStokvelAsync(Stokvel stokvel)
    {
        await _context.Stokvels.AddAsync(stokvel);
        await _context.SaveChangesAsync();
    }

    // Updates an existing stokvel.
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

    // Deletes a stokvel using its ID.
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


    // =========================================================
    // CONTRIBUTION OPERATIONS
    // =========================================================

    // Adds a contribution to the database.
    public async Task AddContributionAsync(Contribution contribution)
    {
        await _context.Contributions.AddAsync(contribution);
        await _context.SaveChangesAsync();
    }

    // Gets one specific contribution.
    public async Task<Contribution?> GetContributionAsync(
        int stokvelId,
        int userId,
        int cycle)
    {
        return await _context.Contributions.FirstOrDefaultAsync(contribution =>
            contribution.StokvelId == stokvelId &&
            contribution.UserId == userId &&
            contribution.Cycle == cycle);
    }

    // Gets all contributions with their member and user.
    public async Task<IReadOnlyCollection<Contribution>> GetContributionsByCycleAsync(
        int stokvelId,
        int cycleId)
    {
        return await _context.Contributions
            .Include(contribution => contribution.Member)
            .ThenInclude(member => member.User)
            .Where(contribution =>
                contribution.StokvelId == stokvelId &&
                contribution.Cycle == cycleId)
            .AsNoTracking()
            .ToListAsync();
    }

    // Gets a filtered, sorted and paged contribution response.
    public async Task<PagedResponse<ContributionResponse>>
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
                : Math.Min(request.PageSize.Value, maximumPageSize);

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
            token = PageToken.Decode(request.PageToken);

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

        var query = _context.Contributions
            .Where(contribution =>
                contribution.StokvelId == stokvelId &&
                contribution.Cycle == cycleId);

        if (request.UserId.HasValue)
        {
            query = query.Where(contribution =>
                contribution.UserId == request.UserId.Value);
        }

        if (sortBy == "userid")
        {
            query = query.OrderBy(contribution =>
                contribution.UserId);

            if (token is not null)
            {
                query = query.Where(contribution =>
                    contribution.UserId > token.LastId);
            }
        }
        else
        {
            query = query
                .OrderBy(contribution =>
                    contribution.ContributionAmount)
                .ThenBy(contribution =>
                    contribution.UserId);

            if (token is not null &&
                token.LastAmount.HasValue)
            {
                var lastAmount = token.LastAmount.Value;
                var lastId = token.LastId;

                query = query.Where(contribution =>
                    contribution.ContributionAmount > lastAmount ||
                    (contribution.ContributionAmount == lastAmount &&
                     contribution.UserId > lastId));
            }
        }

        var results = await query
            .Select(contribution => new ContributionResponse
            {
                UserId = contribution.UserId,
                StokvelId = contribution.StokvelId,
                Cycle = contribution.Cycle,
                // Look up the human-readable number using the cycle's database ID.
                CycleNumber = _context.ContributionCycles
    .Where(cycle => cycle.Id == contribution.Cycle)
    .Select(cycle => cycle.Number)
    .FirstOrDefault(),
                ContributionAmount =
                    contribution.ContributionAmount,
                // Amount in integer cents for the frontend.
                AmountCents = (long)(contribution.ContributionAmount * 100m),

                // Payment status and UTC timestamp.
                Status = contribution.Status,
                PaidAtUtc = contribution.PaidAtUtc,
                MemberName =
                    contribution.Member.User.FirstName + " " +
                    contribution.Member.User.LastName
            })
            .AsNoTracking()
            .Take(pageSize + 1)
            .ToListAsync();

        var hasMore =
            results.Count > pageSize;

        var items =
            results.Take(pageSize).ToList();

        string? nextPageToken = null;

        if (hasMore)
        {
            var lastItem = items[^1];

            nextPageToken = new PageToken
            {
                LastId = lastItem.UserId,
                LastAmount =
                    sortBy == "amount"
                        ? lastItem.ContributionAmount
                        : null,
                SortBy = sortBy,
                Filter = request.UserId?.ToString()
            }.Encode();
        }

        return new PagedResponse<ContributionResponse>
        {
            Items = items,
            NextPageToken = nextPageToken
        };
    }

    // Gets contribution response data using projection.
    public async Task<IReadOnlyCollection<ContributionResponse>>
        GetContributionResponsesByCycleAsync(
            int stokvelId,
            int cycleId)
    {
        return await _context.Contributions
            .Where(contribution =>
                contribution.StokvelId == stokvelId &&
                contribution.Cycle == cycleId)
            .Select(contribution => new ContributionResponse
            {
                UserId = contribution.UserId,
                StokvelId = contribution.StokvelId,
                Cycle = contribution.Cycle,
                // Look up the human-readable number using the cycle's database ID.
                CycleNumber = _context.ContributionCycles
    .Where(cycle => cycle.Id == contribution.Cycle)
    .Select(cycle => cycle.Number)
    .FirstOrDefault(),
                ContributionAmount = contribution.ContributionAmount,
                // Amount in integer cents for the frontend.
                AmountCents = (long)(contribution.ContributionAmount * 100m),

                // Payment status and UTC timestamp.
                Status = contribution.Status,
                PaidAtUtc = contribution.PaidAtUtc,
                MemberName =
                    contribution.Member.User.FirstName + " " +
                    contribution.Member.User.LastName
            })
            .AsNoTracking()
            .ToListAsync();


    }

    // Gets the member and user for a specific contribution.
    public async Task<StokvelMember?> GetMemberForContributionAsync(
        int stokvelId,
        int userId)
    {
        return await _context.StokvelMembers
            .Include(member => member.User)
            .FirstOrDefaultAsync(member =>
                member.StokvelId == stokvelId &&
                member.UserId == userId);
    }


    // =========================================================
    // PAYOUT OPERATIONS
    // =========================================================

    // Adds a payout to the database.
    public async Task AddPayoutAsync(Payout payout)
    {
        await _context.Payouts.AddAsync(payout);
        await _context.SaveChangesAsync();
    }

    // Gets all payouts.
    public async Task<IReadOnlyCollection<Payout>> GetPayoutsAsync()
    {
        return await _context.Payouts
            .AsNoTracking()
            .ToListAsync();
    }

    // Processes a payout and updates the contribution cycle.
    // Processes a payout and updates the contribution cycle.
    public async Task ProcessPayoutAsync(
        Payout payout,
        ContributionCycle cycle)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
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


        });
    }

    // Save a new refresh token.
    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    // Find a refresh token by its SHA-256 hash.
    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    // Retrieve every refresh token in the same family.
    public async Task<List<RefreshToken>> GetRefreshTokensByFamilyAsync(Guid familyId)
    {
        return await _context.RefreshTokens
            .Where(token => token.FamilyId == familyId)
            .ToListAsync();
    }

    // Persist changes to tracked refresh tokens.
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // Atomically consume a refresh token only if it is still valid.
    public async Task<bool> TryUseRefreshTokenAsync(string tokenHash)
    {
        // Update only an unused, unrevoked and unexpired token.
        var updatedRows = await _context.RefreshTokens
            .Where(token =>
                token.TokenHash == tokenHash &&
                !token.IsUsed &&
                !token.IsRevoked &&
                token.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.IsUsed, true));

        // Exactly one request can successfully consume the token.
        return updatedRows == 1;
    }

    // Consume the old token and save its replacement in one transaction.
    public async Task<bool> RotateRefreshTokenAsync(
        string tokenHash,
        RefreshToken replacementToken)
    {
        // Use EF Core's execution strategy for PostgreSQL retries.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Start a database transaction.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Consume the token only if it is still valid and unused.
            var updatedRows = await _context.RefreshTokens
                .Where(token =>
                    token.TokenHash == tokenHash &&
                    !token.IsUsed &&
                    !token.IsRevoked &&
                    token.ExpiresAt > DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.IsUsed, true));

            // Check whether a failed rotation means token reuse.
            if (updatedRows != 1)
            {
                // Read the current database state, not an EF-tracked copy.
                var originalToken = await _context.RefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);

                // Reusing a consumed or revoked token compromises its family.
                if (originalToken is not null &&
                    (originalToken.IsUsed || originalToken.IsRevoked))
                {
                    // Revoke all refresh tokens in this family.
                    await _context.RefreshTokens
                        .Where(token => token.FamilyId == originalToken.FamilyId)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(token => token.IsRevoked, true));
                }

                // Persist the revocation, if any.
                await transaction.CommitAsync();
                return false;
            }

            // Save the replacement token.
            await _context.RefreshTokens.AddAsync(replacementToken);
            await _context.SaveChangesAsync();

            // Commit both changes together.
            await transaction.CommitAsync();

            return true;
        });
    }
}
