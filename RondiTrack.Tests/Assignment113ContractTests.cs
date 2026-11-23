using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RondiTrack.Data;
using RondiTrack.DTOs.Stokvels;

namespace RondiTrack.Tests;

public class Assignment113ContractTests
{
    [Fact]
    public async Task StokvelUpdate_StaleVersionPreservesLatestDetails()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        const string url = "/api/v1/stokvels/1";
        var original = await client.GetFromJsonAsync<StokvelResponse>(url);
        Assert.NotNull(original);

        using var first = await client.PutAsJsonAsync(url, new
        {
            name = "Latest savings group",
            contributionAmount = 450.75m,
            original.Version
        });
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        using var stale = await client.PutAsJsonAsync(url, new
        {
            name = "Stale name",
            contributionAmount = 300m,
            original.Version
        });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains("Review the latest details", await stale.Content.ReadAsStringAsync());

        var latest = await client.GetFromJsonAsync<StokvelResponse>(url);
        Assert.NotNull(latest);
        Assert.NotEqual(original.Version, latest.Version);
        Assert.Equal("Latest savings group", latest.Name);
        Assert.Equal(45075L, latest.ContributionAmountCents);
    }

    [Fact]
    public async Task Payment_ConcurrentRetryAndFreshHostReplayOneContribution()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        using var cycle = await client.PostAsJsonAsync("/api/v1/contributioncycles", new
        {
            id = 11301,
            stokvelId = 1,
            number = 1,
            targetAmount = 5000m
        });
        Assert.Equal(HttpStatusCode.Created, cycle.StatusCode);

        const string paymentUrl = "/api/v1/stokvels/1/cycles/11301/contributions";
        var key = Guid.NewGuid().ToString();
        var responses = await Task.WhenAll(SendAsync(client), SendAsync(client));
        using var original = Assert.Single(responses, response => !WasReplayed(response));
        using var concurrent = Assert.Single(responses, WasReplayed);
        Assert.Equal(HttpStatusCode.Created, original.StatusCode);
        Assert.Equal(HttpStatusCode.Created, concurrent.StatusCode);
        var originalBody = await original.Content.ReadAsStringAsync();
        Assert.Equal(originalBody, await concurrent.Content.ReadAsStringAsync());

        var jwtKey = factory.Services.GetRequiredService<IConfiguration>()["Jwt:Key"];
        await using var freshHost = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Jwt:Key", jwtKey));
        using var freshClient = freshHost.CreateClient();
        freshClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;

        using var replay = await SendAsync(freshClient);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.True(WasReplayed(replay));
        Assert.Equal(originalBody, await replay.Content.ReadAsStringAsync());

        using var changed = await SendAsync(freshClient, 45076);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Contains("Idempotency conflict", await changed.Content.ReadAsStringAsync());

        await using var scope = freshHost.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
        Assert.Equal(1, await db.Contributions.CountAsync(row => row.Cycle == 11301));
        Assert.Equal(1, await db.ContributionReceipts.CountAsync(row => row.Key == key));

        async Task<HttpResponseMessage> SendAsync(HttpClient http, long amount = 45075)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, paymentUrl)
            {
                Content = JsonContent.Create(new { userId = 1, amountInCents = amount })
            };
            request.Headers.Add("Idempotency-Key", key);
            return await http.SendAsync(request);
        }
    }

    private static bool WasReplayed(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Idempotency-Replayed", out var values) &&
        values.Single() == "true";
}
