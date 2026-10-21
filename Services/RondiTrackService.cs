using RondiTrack.Repositories;
using RondiTrack.Models;
using RondiTrack.DTOs.Users;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.Mappers;
using RondiTrack.Idempotency;
using RondiTrack.Exceptions;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.Pagination;
using RondiTrack.Telemetry;
using System.Diagnostics;


namespace RondiTrack.Services;

public class RondiTrackService : IRondiTrackService
{

    // Dependencies used for database operations, idempotency,
    // and obtaining the current time.
    private readonly IRondiTrackRepository _repository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly TimeProvider _timeProvider;

    // The optional TimeProvider preserves compatibility with existing
    // tests that construct RondiTrackService using two arguments.
    public RondiTrackService(
        IRondiTrackRepository repository,
        IIdempotencyStore idempotencyStore,
        TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _idempotencyStore = idempotencyStore;

        // Use the supplied provider when available.
        // Otherwise, fall back to the system clock.
        _timeProvider = timeProvider ?? TimeProvider.System;
    }


    //Add a member to a stokvel
    public async Task AddMemberAsync(int stokvelId, int userId)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);
        if (stokvel is null)
            throw new NotFoundException("Stokvel not found.");

        var user = await _repository.GetUserByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User not found.");

        stokvel.AddMember(user);

        await _repository.UpdateStokvelAsync(stokvel);
    }


    //Remove a member from a stokvel
    public async Task RemoveMemberAsync(int stokvelId, int userId)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);
        if (stokvel is null)
            throw new NotFoundException("Stokvel not found.");

        var user = await _repository.GetUserByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User not found.");

        stokvel.RemoveMember(user);

        await _repository.UpdateStokvelAsync(stokvel);
    }
    public async Task<Contribution> RecordContributionAsync(
    int stokvelId,
    int userId,
    int cycle,
    string idempotencyKey)
    {
        var payload = $"{stokvelId}:{userId}:{cycle}";

        var existingRecord =
            await _idempotencyStore.GetAsync(idempotencyKey);

        if (existingRecord is not null &&
            existingRecord.Payload == payload)
        {
            return existingRecord.Contribution;
        }

        if (existingRecord is not null &&
            existingRecord.Payload != payload)
        {
            throw new IdempotencyConflictException(
                "Idempotency key has already been used with a different request.");
        }

        var stokvel =
            await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            throw new NotFoundException(
                "Stokvel not found.");
        }

        var user =
            await _repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            throw new NotFoundException(
                "User not found.");
        }

        var contributionCycle =
            await _repository.GetContributionCycleByIdAsync(cycle);

        if (contributionCycle is null)
        {
            throw new NotFoundException(
                $"No contribution cycle with ID {cycle} was found.");
        }

        if (contributionCycle.StokvelId != stokvelId)
        {
            throw new BusinessRuleException(
                "The contribution cycle does not belong to this stokvel.");
        }

        if (!stokvel.Members.Any(member => member.UserId == userId))
        {
            throw new BusinessRuleException(
                "User is not a member of this stokvel.");
        }

        var existingContribution =
            await _repository.GetContributionAsync(
                stokvelId,
                userId,
                cycle);

        if (existingContribution is not null)
        {
            throw new BusinessRuleException(
                "A contribution has already been recorded for this member and cycle.");
        }

        // Obtain the payment timestamp from the injected clock.
        // UTC timestamps ensure consistent storage and API responses.
        var paidAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        // Create the contribution with its actual payment timestamp.
        var contribution = new Contribution(
            userId,
            stokvelId,
            cycle,
            stokvel.ContributionAmount,
            paidAtUtc);

        await _repository.AddContributionAsync(contribution);

        var record = new IdempotencyRecord(
            payload,
            contribution);

        await _idempotencyStore.SaveAsync(
     idempotencyKey,
     record);

        // Count only newly recorded contributions.
        // Idempotent retries do not increment the counter.
        RondiTrackTelemetry.ContributionsRecorded.Add(1);

        return contribution;
    }

    public async Task<Payout> ProcessPayoutAsync(
    int stokvelId,
    int contributionCycleId)
    {
        // Create a custom span for the payout business operation.
        using var activity =
            RondiTrackTelemetry.ActivitySource.StartActivity(
                "payout.process");

        // Cycle ID is intentionally not recorded.
        // Only the fixed outcome values are used as tags.
        activity?.SetTag("payout.operation", "process");

        try
        {
            var stokvel =
                await _repository.GetStokvelByIdAsync(stokvelId);

            if (stokvel is null)
            {
                throw new NotFoundException("Stokvel not found.");
            }

            var cycle =
                await _repository.GetContributionCycleByIdAsync(
                    contributionCycleId);

            if (cycle is null)
            {
                throw new NotFoundException(
                    "Contribution cycle not found.");
            }

            if (cycle.StokvelId != stokvelId)
            {
                throw new BusinessRuleException(
                    "The contribution cycle does not belong to this stokvel.");
            }

            if (cycle.Status == "Paid")
            {
                throw new BusinessRuleException(
                    "This contribution cycle has already been paid out.");
            }

            var members = stokvel.Members
                .OrderBy(member => member.UserId)
                .ToList();

            if (members.Count == 0)
            {
                throw new BusinessRuleException(
                    "This stokvel has no members.");
            }

            var rotationIndex =
                (cycle.Number - 1) % members.Count;

            var recipient = members[rotationIndex];

            var contribution =
                await _repository.GetContributionAsync(
                    stokvelId,
                    recipient.UserId,
                    contributionCycleId);

            if (contribution is null)
            {
                throw new BusinessRuleException(
                    "The next member in the rotation has not contributed to this cycle.");
            }

            var payouts =
                await _repository.GetPayoutsAsync();

            var payoutId =
                payouts.Count == 0
                    ? 1
                    : payouts.Max(payout => payout.Id) + 1;

            var payout = new Payout(
                payoutId,
                stokvelId,
                recipient.UserId,
                contributionCycleId,
                stokvel.ContributionAmount * members.Count,
                DateTime.UtcNow,
                rotationIndex + 1);

            // Database operations occur inside the payout span.
            await _repository.ProcessPayoutAsync(
                payout,
                cycle);

            // Record successful payout.
            activity?.SetTag("payout.outcome", "success");

            RondiTrackTelemetry.PayoutsProcessed.Add(
                1,
                new KeyValuePair<string, object?>(
                    "outcome",
                    "success"));

            return payout;
        }
        catch
        {
            // Mark the payout span as failed.
            activity?.SetTag("payout.outcome", "failure");

            activity?.SetStatus(ActivityStatusCode.Error);

            // Count failed payouts without recording sensitive values.
            RondiTrackTelemetry.PayoutsProcessed.Add(
                1,
                new KeyValuePair<string, object?>(
                    "outcome",
                    "failure"));

            throw;
        }
    }
    //Gets all contributions for a specific stokvel and contribution cycle
    public async Task<IReadOnlyCollection<Contribution>> GetContributionsByCycleAsync(
        int stokvelId,
        int cycleId)
    {
        var contributions =
            await _repository.GetContributionsByCycleAsync(
                stokvelId,
                cycleId);

        return contributions;
    }

    //Gets filtered, sorted and paged contributions
    public async Task<PagedResponse<ContributionResponse>>
        GetPagedContributionResponsesByCycleAsync(
            int stokvelId,
            int cycleId,
            ContributionPageRequest request)
    {
        return await _repository.GetPagedContributionResponsesByCycleAsync(
            stokvelId,
            cycleId,
            request);
    }
}

