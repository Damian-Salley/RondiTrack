using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RondiTrack.Tests;

public class EdgeCaseTests
{
    
    [Fact]
    public async Task NewStokvel_WithNoMembers_ReturnsEmptyMemberCollection()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var stokvel = new
        {
            id = 6001,
            name = "Empty Stokvel",
            contributionAmount = 500m
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/stokvels",
                stokvel);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var membersResponse =
            await client.GetAsync(
                "/api/stokvels/6001/members");

        Assert.Equal(
            HttpStatusCode.OK,
            membersResponse.StatusCode);

        var body =
            await membersResponse.Content.ReadAsStringAsync();

        Assert.Equal("[]", body);
    }

    [Fact]
    public async Task CreateStokvel_WithZeroContributionAmount_Returns400()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var client = factory.CreateClient();

        var stokvel = new
        {
            id = 6002,
            name = "Boundary Stokvel",
            contributionAmount = 0m
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/stokvels",
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
            new WebApplicationFactory<Program>();

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
                "/api/contribution-cycles",
                cycle);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
}