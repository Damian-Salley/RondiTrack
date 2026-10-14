using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

namespace RondiTrack.Tests;

public class EdgeCaseTests
{

    [Fact]
    public async Task NewStokvel_CreatorBecomesFirstMember()
    {
        await using var factory =
           new CustomWebApplicationFactory();

        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var stokvel = new
        {
            id = 6001,
            name = "Empty Stokvel",
            contributionAmount = 500m
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/v1/stokvels",
                stokvel);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var membersResponse =
            await client.GetAsync(
                "/api/v1/stokvels/6001/members");

        Assert.Equal(
            HttpStatusCode.OK,
            membersResponse.StatusCode);

        var body =
            await membersResponse.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(body);

        var members = json.RootElement;

        Assert.Equal(
            JsonValueKind.Array,
            members.ValueKind);

        Assert.Single(members.EnumerateArray());
    }

    [Fact]
    public async Task CreateStokvel_WithZeroContributionAmount_Returns400()
    {
        await using var factory =
          new CustomWebApplicationFactory();

        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var stokvel = new
        {
            id = 6002,
            name = "Boundary Stokvel",
            contributionAmount = 0m
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/v1/stokvels",
                stokvel);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateContributionCycle_ForMissingStokvel_Returns404()
    {
        await using var factory =
           new CustomWebApplicationFactory();

        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var cycle = new
        {
            id = 6003,
            stokvelId = 99999,
            number = 1,
            targetAmount = 5000m
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/v1/contributioncycles",
                cycle);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
}

