using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.ContributionCycles;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.DTOs.Users;

namespace RondiTrack.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task Users_HappyPath_CreateAndGetUser()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var user = new
        {
            id = 7001,
            firstName = "Integration",
            lastName = "User",
            email = "integration@example.com",
            phoneNumber = "0821111111"
        };

        var createResponse =
            await client.PostAsJsonAsync("/api/users", user);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var getResponse =
            await client.GetAsync("/api/users/7001");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var result =
            await getResponse.Content.ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(result);
        Assert.Equal(7001, result.Id);
    }

    [Fact]
    public async Task Stokvels_HappyPath_CreateAndGetStokvel()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var stokvel = new
        {
            id = 7001,
            name = "Integration Stokvel",
            contributionAmount = 500m
        };

        var createResponse =
            await client.PostAsJsonAsync("/api/stokvels", stokvel);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var getResponse =
            await client.GetAsync("/api/stokvels/7001");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var result =
            await getResponse.Content.ReadFromJsonAsync<StokvelResponse>();

        Assert.NotNull(result);
        Assert.Equal(7001, result.Id);
    }

    [Fact]
    public async Task ContributionCycles_HappyPath_CreateAndGetCycle()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var cycle = new
        {
            id = 7001,
            stokvelId = 1,
            number = 7,
            targetAmount = 5000m
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/contribution-cycles",
                cycle);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var getResponse =
            await client.GetAsync(
                "/api/contribution-cycles/7001");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var result =
            await getResponse.Content
                .ReadFromJsonAsync<ContributionCycleResponse>();

        Assert.NotNull(result);
        Assert.Equal(7001, result.Id);
    }

    [Fact]
    public async Task Contribution_SameIdempotencyKeyAndPayload_ReturnsIdenticalResponse()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var cycle = new
        {
            id = 8001,
            stokvelId = 1,
            number = 8,
            targetAmount = 5000m
        };

        var cycleResponse =
            await client.PostAsJsonAsync(
                "/api/contribution-cycles",
                cycle);

        Assert.Equal(
            HttpStatusCode.Created,
            cycleResponse.StatusCode);

        var request = new
        {
            cycle = 8001
        };

        using var firstRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/stokvels/1/members/1/contributions");

        firstRequest.Headers.Add(
            "Idempotency-Key",
            "integration-same-key");

        firstRequest.Content =
            JsonContent.Create(request);

        var firstResponse =
            await client.SendAsync(firstRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var firstBody =
            await firstResponse.Content.ReadAsStringAsync();

        using var secondRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/stokvels/1/members/1/contributions");

        secondRequest.Headers.Add(
            "Idempotency-Key",
            "integration-same-key");

        secondRequest.Content =
            JsonContent.Create(request);

        var secondResponse =
            await client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            secondResponse.StatusCode);

        var secondBody =
            await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(firstBody, secondBody);
    }

    [Fact]
    public async Task Contribution_SameIdempotencyKeyDifferentPayload_Returns409()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/contribution-cycles",
            new
            {
                id = 8101,
                stokvelId = 1,
                number = 81,
                targetAmount = 5000m
            });

        await client.PostAsJsonAsync(
            "/api/contribution-cycles",
            new
            {
                id = 8102,
                stokvelId = 1,
                number = 82,
                targetAmount = 5000m
            });

        using var firstRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/stokvels/1/members/1/contributions");

        firstRequest.Headers.Add(
            "Idempotency-Key",
            "integration-conflict-key");

        firstRequest.Content =
            JsonContent.Create(new { cycle = 8101 });

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
            "integration-conflict-key");

        secondRequest.Content =
            JsonContent.Create(new { cycle = 8102 });

        var secondResponse =
            await client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        Assert.Equal(
            "application/problem+json",
            secondResponse.Content.Headers.ContentType?.MediaType);
    }
}