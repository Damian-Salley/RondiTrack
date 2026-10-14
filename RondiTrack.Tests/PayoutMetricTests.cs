using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RondiTrack.Data;
using RondiTrack.Models;
using RondiTrack.Tests.TestSupport;

namespace RondiTrack.Tests;

[Collection("Postgres collection")]
public class PayoutMetricTests
{
    private readonly PostgresApiFactory _factory;

    public PayoutMetricTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SuccessfulPayout_IncrementsSuccessCounter()
    {
        // Listen for payout metrics before making the request.
        long successCount = 0;

        using var listener = new MeterListener();

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "RondiTrack.Business" &&
                instrument.Name == "ronditrack.payouts.processed")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>(
            (instrument, measurement, tags, state) =>
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "outcome" &&
                        tag.Value?.ToString() == "success")
                    {
                        Interlocked.Add(ref successCount, measurement);
                    }
                }
            });

        listener.Start();

        var id = Random.Shared.Next(100000, 1000000000);

        const string password = "CorrectPassword123!";
        var email = $"payout-metric-{id}@example.com";

        using var client = _factory.CreateClient();

        // Register a user.
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                id,
                firstName = "Test",
                lastName = "Treasurer",
                email,
                phoneNumber = "0821234567",
                password
            });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Create a stokvel, membership and contribution cycle.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Stokvels.Add(new Stokvel(
                id,
                "Payout Metric Test",
                500m));

            db.StokvelMembers.Add(new StokvelMember(
                id,
                id,
                "Treasurer",
                DateTime.UtcNow));

            db.ContributionCycles.Add(new ContributionCycle(
                id,
                id,
                1,
                500m));

            await db.SaveChangesAsync();
        }

        // Log in.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var accessToken = loginJson.RootElement
            .GetProperty("accessToken")
            .GetString()!;

        // Record the contribution required for payout.
        using var contributionRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{id}/members/{id}/contributions");

        contributionRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        contributionRequest.Headers.Add(
            "Idempotency-Key",
            $"payout-metric-{id}");

        contributionRequest.Content = JsonContent.Create(new
        {
            cycle = id
        });

        using var contributionResponse =
            await client.SendAsync(contributionRequest);

        Assert.Equal(HttpStatusCode.OK, contributionResponse.StatusCode);

        // Process the payout.
        using var payoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{id}/cycles/{id}/payout");

        payoutRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        using var payoutResponse =
            await client.SendAsync(payoutRequest);

        Assert.Equal(HttpStatusCode.OK, payoutResponse.StatusCode);

        // Verify that a successful payout incremented the counter.
        Assert.True(
            Interlocked.Read(ref successCount) >= 1,
            "Expected the payout success counter to increase.");
    }
}