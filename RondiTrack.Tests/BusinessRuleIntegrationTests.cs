using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RondiTrack.Tests;

public class BusinessRuleIntegrationTests
{
    [Fact]
    public async Task DuplicateStokvel_Returns422()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

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
            new WebApplicationFactory<Program>();

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
            new WebApplicationFactory<Program>();

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
            new WebApplicationFactory<Program>();

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
            new WebApplicationFactory<Program>();

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
            new WebApplicationFactory<Program>();

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
            new WebApplicationFactory<Program>();

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
}