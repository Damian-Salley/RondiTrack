using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RondiTrack.Data;
using RondiTrack.Models;
using RondiTrack.Tests.TestSupport;

namespace RondiTrack.Tests;

[Collection("Postgres collection")]
public class PopiaLoggingTests
{
    private readonly PostgresApiFactory _factory;

    public PopiaLoggingTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthenticationAndContribution_DoNotLeakSensitiveInformation()
    {
        using var client = _factory.CreateClient();

        var id = Random.Shared.Next(100000, 1000000000);

        const string password = "UniqueSecretPassword123!";
        const string incorrectPassword = "IncorrectSecretPassword123!";

        var email = $"popia-secret-{id}@example.com";

        // Use a distinctive financial amount.
        const decimal contributionAmount = 987654.32m;

        // Clear logs from earlier tests.
        _factory.CapturedLogs.Clear();

        // Register a user.
        var registration = new
        {
            id,
            firstName = "Test",
            lastName = "User",
            email,
            phoneNumber = "0821234567",
            password
        };

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            registration);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Capture logs before the duplicate registration.
        var logsBeforeDuplicate = _factory.CapturedLogs.Logs.Count;

        // Duplicate registration must return 409.
        using var duplicateResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            registration);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        // Verify that the duplicate request produced no Error logs.
        var duplicateLogs = _factory.CapturedLogs.Logs
            .Skip(logsBeforeDuplicate)
            .ToArray();

        Assert.DoesNotContain(
            duplicateLogs,
            log => log.Level >= LogLevel.Error);

        // Successful login.
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var accessToken = loginJson.RootElement
            .GetProperty("accessToken")
            .GetString()!;

        // Capture the refresh token before rotation.
        var originalRefreshToken = loginResponse.Headers
            .GetValues("Set-Cookie")
            .First(value => value.StartsWith("refreshToken="))
            .Split(';')[0]
            .Substring("refreshToken=".Length);

        // Failed login.
        using var failedLoginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = incorrectPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, failedLoginResponse.StatusCode);

        // Refresh the authentication session.
        using var refreshRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/refresh");

        refreshRequest.Headers.Add(
            "Cookie",
            $"refreshToken={originalRefreshToken}");

        using var refreshResponse = await client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        using var refreshJson = JsonDocument.Parse(
            await refreshResponse.Content.ReadAsStringAsync());

        var refreshedAccessToken = refreshJson.RootElement
            .GetProperty("accessToken")
            .GetString()!;

        // Capture the replacement refresh token.
        var replacementRefreshToken = refreshResponse.Headers
            .GetValues("Set-Cookie")
            .First(value => value.StartsWith("refreshToken="))
            .Split(';')[0]
            .Substring("refreshToken=".Length);

        // Seed a stokvel and contribution cycle.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Stokvels.Add(new Stokvel(
                id,
                "POPIA Logging Test",
                contributionAmount));

            db.StokvelMembers.Add(new StokvelMember(
                id,
                id,
                "Member",
                DateTime.UtcNow));

            db.ContributionCycles.Add(new ContributionCycle(
                id,
                id,
                1,
                contributionAmount));

            await db.SaveChangesAsync();
        }

        // Record a contribution.
        using var contributionRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{id}/members/{id}/contributions");

        contributionRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshedAccessToken);

        contributionRequest.Headers.Add(
            "Idempotency-Key",
            $"popia-test-{id}");

        contributionRequest.Content = JsonContent.Create(new
        {
            cycle = id
        });

        using var contributionResponse =
            await client.SendAsync(contributionRequest);

        Assert.Equal(HttpStatusCode.OK, contributionResponse.StatusCode);

        // Check all captured logs for sensitive values.
        var logs = _factory.CapturedLogs.Logs.ToArray();

        var combinedLogs = string.Join(
            Environment.NewLine,
            logs.Select(log => log.Message));

        Assert.DoesNotContain(email, combinedLogs);
        Assert.DoesNotContain(password, combinedLogs);
        Assert.DoesNotContain(incorrectPassword, combinedLogs);

        Assert.DoesNotContain(accessToken, combinedLogs);
        Assert.DoesNotContain(refreshedAccessToken, combinedLogs);

        Assert.DoesNotContain(originalRefreshToken, combinedLogs);
        Assert.DoesNotContain(replacementRefreshToken, combinedLogs);

        Assert.DoesNotContain(
            contributionAmount.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            combinedLogs);
    }
}