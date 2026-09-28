using RondiTrack.Exceptions;
using RondiTrack.Idempotency;
using RondiTrack.Models;
using RondiTrack.Repositories;
using RondiTrack.Services;

namespace RondiTrack.Tests;

public class BusinessRuleTests
{
    [Fact]
    public void AddMember_WhenUserAlreadyMember_ThrowsBusinessRuleException()
    {
        var stokvel = new Stokvel(1, "Test Stokvel", 500m);
        var user = new User(
            1,
            "Damian",
            "Salley",
            "damian@example.com",
            "0821234567");

        stokvel.AddMember(user);

        Assert.Throws<BusinessRuleException>(
            () => stokvel.AddMember(user));
    }

    [Fact]
    public void RemoveMember_WhenUserIsNotMember_ThrowsBusinessRuleException()
    {
        var stokvel = new Stokvel(1, "Test Stokvel", 500m);
        var user = new User(
            99,
            "Test",
            "User",
            "test@example.com",
            "0820000000");

        Assert.Throws<BusinessRuleException>(
            () => stokvel.RemoveMember(user));
    }

    [Fact]
    public async Task RecordContribution_WhenContributionAlreadyExists_ThrowsBusinessRuleException()
    {
        var repository = new InMemoryRondiTrackRepository();
        var idempotencyStore = new InMemoryIdempotencyStore();

        var service = new RondiTrackService(
            repository,
            idempotencyStore);

        var cycle = new ContributionCycle(
            101,
            1,
            1,
            5000m);

        await repository.AddContributionCycleAsync(cycle);

        await service.RecordContributionAsync(
            1,
            1,
            101,
            "first-key");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.RecordContributionAsync(
                1,
                1,
                101,
                "different-key"));
    }

    [Fact]
    public async Task RecordContribution_SameIdempotencyKeyAndPayload_ReturnsOriginalContribution()
    {
        var repository = new InMemoryRondiTrackRepository();
        var idempotencyStore = new InMemoryIdempotencyStore();

        var service = new RondiTrackService(
            repository,
            idempotencyStore);

        var cycle = new ContributionCycle(
            101,
            1,
            1,
            5000m);

        await repository.AddContributionCycleAsync(cycle);

        var first = await service.RecordContributionAsync(
            1,
            1,
            101,
            "same-key");

        var second = await service.RecordContributionAsync(
            1,
            1,
            101,
            "same-key");

        Assert.Same(first, second);
    }

    [Fact]
    public async Task RecordContribution_SameIdempotencyKeyDifferentPayload_ThrowsIdempotencyConflictException()
    {
        var repository = new InMemoryRondiTrackRepository();
        var idempotencyStore = new InMemoryIdempotencyStore();

        var service = new RondiTrackService(
            repository,
            idempotencyStore);

        var cycle101 = new ContributionCycle(
            101,
            1,
            1,
            5000m);

        var cycle102 = new ContributionCycle(
            102,
            1,
            2,
            5000m);

        await repository.AddContributionCycleAsync(cycle101);
        await repository.AddContributionCycleAsync(cycle102);

        await service.RecordContributionAsync(
            1,
            1,
            101,
            "reused-key");

        await Assert.ThrowsAsync<IdempotencyConflictException>(
            () => service.RecordContributionAsync(
                1,
                1,
                102,
                "reused-key"));
    }
}