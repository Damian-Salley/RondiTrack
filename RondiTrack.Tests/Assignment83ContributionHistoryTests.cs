using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RondiTrack.DTOs.Contributions;
using RondiTrack.DTOs.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RondiTrack.Data;
using RondiTrack.Models;

namespace RondiTrack.Tests;

public class Assignment83ContributionHistoryTests
{
    private const string HistoryUrl =
        "/api/v1/stokvels/1/contributions";

    [Fact]
    public async Task History_NoContributions_ReturnsEmptyPage()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(HistoryUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<
            PagedResponse<ContributionResponse>>();

        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Null(page.NextPageToken);
    }

    [Fact]
    public async Task History_InvalidPageToken_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"{HistoryUrl}?pageToken=invalid-token");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task History_InvalidDateRange_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var from = Uri.EscapeDataString(
            "2026-11-01T00:00:00Z");

        var to = Uri.EscapeDataString(
            "2026-10-01T00:00:00Z");

        var response = await client.GetAsync(
            $"{HistoryUrl}?from={from}&to={to}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task History_InvalidMemberFilter_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"{HistoryUrl}?userId=-1");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task History_UnknownMember_Returns404()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"{HistoryUrl}?userId=999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task History_NonexistentStokvel_Returns404()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/stokvels/999999/contributions");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task History_IncludesContributionsFromMultipleCycles()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        // Create two contribution cycles.
        await CreateTestCycleAsync(client, 8301);
        await CreateTestCycleAsync(client, 8302);

        // Insert contributions with different timestamps.
        await InsertTestContributionsAsync(
            factory,
            new Contribution(
                1, 1, 8301, 500m,
                new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)),
            new Contribution(
                1, 1, 8302, 500m,
                new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc)));

        var response = await client.GetAsync(HistoryUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<
            PagedResponse<ContributionResponse>>();

        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);

        // Newest contribution must appear first.
        Assert.Equal(8302, page.Items.First().Cycle);
        Assert.Equal(8301, page.Items.Last().Cycle);
    }

    [Fact]
    public async Task History_DateFilters_ReturnOnlyMatchingContributions()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        await CreateTestCycleAsync(client, 8311);
        await CreateTestCycleAsync(client, 8312);

        await InsertTestContributionsAsync(
            factory,
            new Contribution(
                1, 1, 8311, 500m,
                new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc)),
            new Contribution(
                1, 1, 8312, 500m,
                new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc)));

        var response = await client.GetAsync(
            $"{HistoryUrl}?from=2026-10-01T00%3A00%3A00Z" +
            "&to=2026-11-01T00%3A00%3A00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<
            PagedResponse<ContributionResponse>>();

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(8312, page.Items.Single().Cycle);
    }

    [Fact]
    public async Task History_Pagination_ReturnsDistinctPages()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        await CreateTestCycleAsync(client, 8321);
        await CreateTestCycleAsync(client, 8322);
        await CreateTestCycleAsync(client, 8323);

        await InsertTestContributionsAsync(
            factory,
            new Contribution(
                1, 1, 8321, 500m,
                new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)),
            new Contribution(
                1, 1, 8322, 500m,
                new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc)),
            new Contribution(
                1, 1, 8323, 500m,
                new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc)));

        // Request only two records.
        var firstResponse = await client.GetAsync(
            $"{HistoryUrl}?pageSize=2");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstPage = await firstResponse.Content.ReadFromJsonAsync<
            PagedResponse<ContributionResponse>>();

        Assert.NotNull(firstPage);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.NotNull(firstPage.NextPageToken);

        // Request the next page using the returned token.
        var secondResponse = await client.GetAsync(
            $"{HistoryUrl}?pageSize=2&pageToken=" +
            Uri.EscapeDataString(firstPage.NextPageToken!));

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondPage = await secondResponse.Content.ReadFromJsonAsync<
            PagedResponse<ContributionResponse>>();

        Assert.NotNull(secondPage);
        Assert.Single(secondPage.Items);
        Assert.Null(secondPage.NextPageToken);

        // Verify no contribution appears on both pages.
        var allCycles = firstPage.Items
            .Concat(secondPage.Items)
            .Select(item => item.Cycle)
            .ToList();

        Assert.Equal(3, allCycles.Distinct().Count());
    }

    [Fact]
    public async Task History_ChangingFiltersWithExistingToken_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        await CreateTestCycleAsync(client, 8331);
        await CreateTestCycleAsync(client, 8332);

        await InsertTestContributionsAsync(
            factory,
            new Contribution(
                1, 1, 8331, 500m,
                new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)),
            new Contribution(
                1, 1, 8332, 500m,
                new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc)));

        var firstResponse = await client.GetAsync(
            $"{HistoryUrl}?pageSize=1");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstPage = await firstResponse.Content.ReadFromJsonAsync<
            PagedResponse<ContributionResponse>>();

        Assert.NotNull(firstPage);
        Assert.NotNull(firstPage.NextPageToken);

        // Reuse the token with a different date filter.
        var response = await client.GetAsync(
            $"{HistoryUrl}?pageSize=1" +
            "&from=2026-10-01T00%3A00%3A00Z" +
            "&pageToken=" +
            Uri.EscapeDataString(firstPage.NextPageToken!));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Creates a contribution cycle using the existing API.
    private static async Task CreateTestCycleAsync(
        HttpClient client,
        int cycleId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/contributioncycles",
            new
            {
                id = cycleId,
                stokvelId = 1,
                number = cycleId,
                targetAmount = 5000m
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // Inserts contributions directly into the test database.
    // This allows us to control their payment timestamps.
    private static async Task InsertTestContributionsAsync(
        CustomWebApplicationFactory factory,
        params Contribution[] contributions)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        db.Contributions.AddRange(contributions);

        await db.SaveChangesAsync();
    }
}