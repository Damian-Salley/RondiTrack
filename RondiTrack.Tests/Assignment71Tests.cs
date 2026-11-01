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

// Integration tests for the Assignment 7.1 dashboard API.
//
// Uses the existing ASP.NET Core test application and
// isolated PostgreSQL Testcontainer.
[Collection("Postgres collection")]
public class Assignment71Tests
{
    private readonly PostgresApiFactory _factory;
    private readonly HttpClient _client;

    public Assignment71Tests(PostgresApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Verify that contribution data cannot be accessed
    // without an authenticated user.
    [Fact]
    public async Task Contributions_WithoutAuthentication_Returns401()
    {
        using var response = await _client.GetAsync(
            "/api/v1/stokvels/9002/contributions");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // Verify the data required by ContributionCard:
    // integer cents, payment status, cycle number,
    // member name and UTC payment timestamp.
    [Fact]
    public async Task Contributions_ReturnCorrectDashboardFields()
    {
        // Arrange: Generate a unique value to avoid conflicts
        // with other integration tests.
        var id = Random.Shared.Next(100000, 100000000);

        // These IDs are assigned by PostgreSQL.
        int userId;
        int stokvelId;
        int cycleId;

        const string password = "CorrectPassword123!";
        var email = $"assignment71-{id}@example.com";

        // Register the member through the real API.
        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                id,
                firstName = "Assignment",
                lastName = "Member",
                email,
                phoneNumber = "0821234567",
                password
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        // Create the stokvel, membership, cycle and contribution
        // directly in the isolated test database.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            // Retrieve the actual registered user ID.
            userId = await db.Users
                .Where(user => user.Email == email)
                .Select(user => user.Id)
                .SingleAsync();

            // Save the stokvel first.
            var stokvel = new Stokvel(
                id,
                "Assignment 7.1 Test Stokvel",
                250m);

            db.Stokvels.Add(stokvel);

            await db.SaveChangesAsync();

            stokvelId = stokvel.Id;

            // Create the membership using valid foreign keys.
            db.StokvelMembers.Add(new StokvelMember(
                stokvelId,
                userId,
                "Member",
                DateTime.UtcNow));

            // Deliberately use different values for cycle ID
            // and cycle number to verify their meanings.
            var cycle = new ContributionCycle(
                id + 1,
                stokvelId,
                1,
                250m,
                requiredContributionAmount: 250m);

            db.ContributionCycles.Add(cycle);

            await db.SaveChangesAsync();

            cycleId = cycle.Id;

            // This timestamp is deliberately close to midnight
            // in South African time.
            var paidAtUtc = new DateTime(
                2026, 10, 31, 22, 30, 0,
                DateTimeKind.Utc);

            db.Contributions.Add(new Contribution(
                userId,
                stokvelId,
                cycleId,
                250m,
                paidAtUtc));

            await db.SaveChangesAsync();
        }

        // Authenticate the member.
        using var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = loginJson.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        // Request the contribution endpoint.
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/stokvels/{stokvelId}/contributions");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        // The endpoint returns a paginated response.
        var items = json.RootElement.GetProperty("items");

        Assert.Equal(1, items.GetArrayLength());

        var contribution = items[0];

        // The database cycle ID must remain unchanged.
        Assert.Equal(
            cycleId,
            contribution.GetProperty("cycle").GetInt32());

        // The dashboard displays the human-readable cycle number.
        Assert.Equal(
            1,
            contribution.GetProperty("cycleNumber").GetInt32());

        // R250 must be represented as 25,000 integer cents.
        Assert.Equal(
            25000,
            contribution.GetProperty("amountCents").GetInt64());

        Assert.Equal(
            "Paid",
            contribution.GetProperty("status").GetString());

        Assert.Equal(
            "Assignment Member",
            contribution.GetProperty("memberName").GetString());

        // Verify the UTC instant rather than comparing
        // potentially different timestamp string formats.
        var returnedTimestamp = contribution
            .GetProperty("paidAtUtc")
            .GetDateTimeOffset();

        Assert.Equal(
            new DateTimeOffset(
                2026, 10, 31, 22, 30, 0,
                TimeSpan.Zero),
            returnedTimestamp);
    }

    // Verify that a pending contribution has no payment
    // timestamp and does not count as money already paid.
    [Fact]
    public void PendingContribution_HasNoPaymentTimestamp()
    {
        // Arrange
        var contribution = new Contribution(
            userId: 1,
            stokvelId: 1,
            cycle: 1,
            contributionAmount: 100m);

        // Act
        contribution.MarkAsPending();

        // Assert
        Assert.Equal("Pending", contribution.Status);
        Assert.Null(contribution.PaidAtUtc);
    }

    // Verify that a confirmed contribution retains its
    // amount and original UTC payment timestamp.
    [Fact]
    public void ConfirmedContribution_PreservesPaymentDetails()
    {
        // Arrange: R0.10 with a known UTC payment time.
        var paidAtUtc = new DateTime(
            2026, 10, 20, 23, 54, 25,
            DateTimeKind.Utc);

        var contribution = new Contribution(
            userId: 1,
            stokvelId: 1,
            cycle: 1,
            contributionAmount: 0.10m,
            paidAtUtc: paidAtUtc);

        // Act
        contribution.Confirm();

        // Assert
        Assert.Equal("Confirmed", contribution.Status);
        Assert.Equal(0.10m, contribution.ContributionAmount);
        Assert.Equal(paidAtUtc, contribution.PaidAtUtc);

        // Verify that R0.10 converts to exactly 10 cents.
        var amountCents = checked(
            (long)(contribution.ContributionAmount * 100m));

        Assert.Equal(10L, amountCents);
    }
}