using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RondiTrack.Data;
using RondiTrack.Models;
using RondiTrack.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace RondiTrack.Tests;

public class BusinessRuleIntegrationTests
{
    [Fact]
    public async Task DuplicateStokvel_Returns422()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var stokvel = new
        {
            id = 5001,
            name = "Duplicate Test",
            contributionAmount = 500m
        };

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/stokvels",
                stokvel);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/stokvels",
                stokvel);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task DuplicateContributionCycle_Returns422()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var cycle = new
        {
            id = 5002,
            stokvelId = 1,
            number = 5,
            targetAmount = 5000m
        };

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/contribution-cycles",
                cycle);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/contribution-cycles",
                cycle);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task AddExistingMember_Returns422()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // User 1 is already a member of seeded stokvel 1.
        var response =
            await client.PostAsync(
                "/api/stokvels/1/members/1",
                null);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);
    }

    [Fact]
    public async Task RemoveNonMember_Returns422()
    {
        await using var factory =
           new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var user = new
        {
            id = 5003,
            firstName = "Non",
            lastName = "Member",
            email = "nonmember@example.com",
            phoneNumber = "0822222222"
        };

        var createUserResponse =
            await client.PostAsJsonAsync(
                "/api/users",
                user);

        Assert.Equal(
            HttpStatusCode.Created,
            createUserResponse.StatusCode);

        var response =
            await client.DeleteAsync(
                "/api/stokvels/1/members/5003");

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);
    }

    [Fact]
    public async Task ContributionByNonMember_Returns422()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/users",
            new
            {
                id = 5004,
                firstName = "Another",
                lastName = "NonMember",
                email = "another@example.com",
                phoneNumber = "0823333333"
            });

        await client.PostAsJsonAsync(
            "/api/contribution-cycles",
            new
            {
                id = 5004,
                stokvelId = 1,
                number = 50,
                targetAmount = 5000m
            });

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/stokvels/1/members/5004/contributions");

        request.Headers.Add(
            "Idempotency-Key",
            "non-member-key");

        request.Content =
            JsonContent.Create(new { cycle = 5004 });

        var response =
            await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);
    }

    [Fact]
    public async Task ContributionWithCycleFromDifferentStokvel_Returns422()
    {
        await using var factory =
           new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/stokvels",
            new
            {
                id = 5005,
                name = "Other Stokvel",
                contributionAmount = 500m
            });

        await client.PostAsJsonAsync(
            "/api/contribution-cycles",
            new
            {
                id = 5005,
                stokvelId = 5005,
                number = 51,
                targetAmount = 5000m
            });

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/stokvels/1/members/1/contributions");

        request.Headers.Add(
            "Idempotency-Key",
            "wrong-stokvel-key");

        request.Content =
            JsonContent.Create(new { cycle = 5005 });

        var response =
            await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);
    }

    [Fact]
    public async Task DuplicateContributionWithDifferentKey_Returns422()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/contribution-cycles",
            new
            {
                id = 5006,
                stokvelId = 1,
                number = 52,
                targetAmount = 5000m
            });

        using var firstRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/stokvels/1/members/1/contributions");

        firstRequest.Headers.Add(
            "Idempotency-Key",
            "contribution-key-one");

        firstRequest.Content =
            JsonContent.Create(new { cycle = 5006 });

        var firstResponse =
            await client.SendAsync(firstRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        using var secondRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/stokvels/1/members/1/contributions");

        secondRequest.Headers.Add(
            "Idempotency-Key",
            "contribution-key-two");

        secondRequest.Content =
            JsonContent.Create(new { cycle = 5006 });

        var secondResponse =
            await client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task ProcessPayout_CreatesPayoutAndMarksCycleAsPaid()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // Create a contribution cycle.
        await client.PostAsJsonAsync(
            "/api/contribution-cycles",
            new
            {
                id = 6001,
                stokvelId = 1,
                number = 1,
                targetAmount = 1500m
            });

        // User 1 is the first member in the rotation.
        // Record their contribution.
        using var contributionRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/stokvels/1/members/1/contributions");

        contributionRequest.Headers.Add(
            "Idempotency-Key",
            "payout-test-contribution");

        contributionRequest.Content =
            JsonContent.Create(new { cycle = 6001 });

        var contributionResponse =
            await client.SendAsync(contributionRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            contributionResponse.StatusCode);

        // Process the payout.
        var payoutResponse =
            await client.PostAsync(
                "/api/stokvels/1/cycles/6001/payout",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            payoutResponse.StatusCode);

        var payout =
            await payoutResponse.Content.ReadFromJsonAsync<Payout>();

        Assert.NotNull(payout);
        Assert.Equal(1, payout.UserId);
        Assert.Equal(1, payout.StokvelId);
        Assert.Equal(6001, payout.ContributionCycleId);
        Assert.Equal(1500m, payout.Amount);
        Assert.Equal(1, payout.RotationOrder);
    }

    [Fact]
    public async Task ProcessPayout_WhenPayoutInsertFails_RollsBackCycleUpdate()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // Create a contribution cycle.
        await client.PostAsJsonAsync(
            "/api/contribution-cycles",
            new
            {
                id = 6002,
                stokvelId = 1,
                number = 1,
                targetAmount = 1500m
            });

        using var scope =
            factory.Services.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IRondiTrackRepository>();

        var cycle =
            await context.ContributionCycles
                .FindAsync(6002);

        Assert.NotNull(cycle);
        Assert.Equal("Active", cycle.Status);

        // Create an invalid payout.
        // User 999999 does not exist, so the foreign-key
        // constraint will cause the payout insert to fail.
        var invalidPayout = new Payout(
            9999,
            1,
            999999,
            6002,
            1500m,
            DateTime.UtcNow,
            1);

        await Assert.ThrowsAnyAsync<Exception>(
            async () =>
                await repository.ProcessPayoutAsync(
                    invalidPayout,
                    cycle));

        // Re-query the database to prove the cycle update
        // was rolled back.
        context.ChangeTracker.Clear();

        var cycleAfterFailure =
            await context.ContributionCycles
                .FindAsync(6002);

        Assert.NotNull(cycleAfterFailure);

        Assert.Equal(
            "Active",
            cycleAfterFailure.Status);

        // Prove that the invalid payout was not persisted.
        var payoutAfterFailure =
            await context.Payouts
                .FindAsync(9999);

        Assert.Null(payoutAfterFailure);
    }
}