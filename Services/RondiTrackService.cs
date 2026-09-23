using RondiTrack.Repositories;
using RondiTrack.Models;
using RondiTrack.DTOs.Users;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.Mappers;
using RondiTrack.Idempotency;

namespace RondiTrack.Services;

public class RondiTrackService : IRondiTrackService
{

    private readonly IRondiTrackRepository _repository;
    private readonly IIdempotencyStore _idempotencyStore;

    public RondiTrackService(
    IRondiTrackRepository repository,
    IIdempotencyStore idempotencyStore)
    {
        _repository = repository;
        _idempotencyStore = idempotencyStore;
    }


    //Add a member to a stokvel
    public async Task AddMemberAsync(int stokvelId, int userId)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            throw new InvalidOperationException("Stokvel not found.");
        }

        var user = await _repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        stokvel.AddMember(user);
    }


    //Remove a member from a stokvel
    public async Task RemoveMemberAsync(int stokvelId, int userId)
    {
        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            throw new InvalidOperationException("Stokvel not found.");
        }

        var user = await _repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        stokvel.RemoveMember(user);
    }

    public async Task<Contribution> RecordContributionAsync(
    int stokvelId,
    int userId,
    int cycle,
    string idempotencyKey)
    {
        var payload = $"{stokvelId}:{userId}:{cycle}";

        var existingRecord = await _idempotencyStore.GetAsync(idempotencyKey);

        // Same key + same payload = return original result
        if (existingRecord is not null &&
            existingRecord.Payload == payload)
        {
            return existingRecord.Contribution;
        }

        // Same key + different payload = reject
        if (existingRecord is not null &&
            existingRecord.Payload != payload)
        {
            throw new InvalidOperationException(
                "Idempotency key has already been used with a different request.");
        }

        var stokvel = await _repository.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            throw new InvalidOperationException("Stokvel not found.");
        }

        var user = await _repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!stokvel.Members.Any(u => u.Id == userId))
        {
            throw new InvalidOperationException(
                "User is not a member of this stokvel.");
        }

        var existingContribution = await _repository.GetContributionAsync(
            stokvelId,
            userId,
            cycle);

        if (existingContribution is not null)
        {
            throw new InvalidOperationException(
                "A contribution has already been recorded for this member and cycle.");
        }

        var contribution = new Contribution(
            userId,
            stokvelId,
            cycle,
            stokvel.ContributionAmount
        );

        await _repository.AddContributionAsync(contribution);

        var record = new IdempotencyRecord(
            payload,
            contribution
        );

        await _idempotencyStore.SaveAsync(
            idempotencyKey,
            record
        );

        return contribution;
    }
}