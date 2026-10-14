using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RondiTrack.Data;
using RondiTrack.Models;
using RondiTrack.Tests.TestSupport;

namespace RondiTrack.Tests;

[Collection("Postgres collection")]
public class FrontendReadinessTests
{
    private readonly PostgresApiFactory _factory;
    private readonly HttpClient _client;

    public FrontendReadinessTests(PostgresApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Verify that browser preflight requests are allowed.
    [Fact]
    public async Task Cors_PreflightFromAllowedOrigin_Succeeds()
    {
        // Arrange: Simulate a browser's CORS preflight request.
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/v1/stokvels");

        request.Headers.Add(
            "Origin",
            "http://localhost:3000");

        request.Headers.Add(
            "Access-Control-Request-Method",
            "POST");

        request.Headers.Add(
            "Access-Control-Request-Headers",
            "content-type,authorization");

        // Act
        using var response = await _client.SendAsync(request);

        // Assert: The preflight request must succeed.
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        // The configured frontend origin must be allowed.
        Assert.Equal(
            "http://localhost:3000",
            response.Headers.GetValues(
                "Access-Control-Allow-Origin").Single());

        // Credentialed requests must be supported.
        Assert.Equal(
            "true",
            response.Headers.GetValues(
                "Access-Control-Allow-Credentials").Single());
    }

    // Verify that authentication errors include CORS headers.
    [Fact]
    public async Task Cors_UnauthorizedRequest_IncludesAllowedOrigin()
    {
        // Arrange: Request a protected endpoint without a token.
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/stokvels");

        request.Headers.Add(
            "Origin",
            "http://localhost:3000");

        // Act
        using var response = await _client.SendAsync(request);

        // Assert: Authentication is required.
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        // The browser must be able to read the error response.
        Assert.Equal(
            "http://localhost:3000",
            response.Headers.GetValues(
                "Access-Control-Allow-Origin").Single());

        // Authentication errors must use ProblemDetails.
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    // Verify that authorization errors also include CORS headers.
    [Fact]
    public async Task Cors_ForbiddenRequest_IncludesAllowedOrigin()
    {
        // Arrange: Create a unique user and stokvel.
        var id = Random.Shared.Next(100000, 1000000000);

        const string password = "CorrectPassword123!";

        var email = $"cors-member-{id}@example.com";

        // Register a regular member.
        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                id,
                firstName = "Cors",
                lastName = "Member",
                email,
                phoneNumber = "0821234567",
                password
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        // Create a stokvel and assign the user the Member role.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Stokvels.Add(new Stokvel(
                id,
                "CORS Authorization Test",
                500m));

            db.StokvelMembers.Add(new StokvelMember(
                id,
                id,
                "Member",
                DateTime.UtcNow));

            db.ContributionCycles.Add(new ContributionCycle(
                id,
                id,
                1,
                500m));

            await db.SaveChangesAsync();
        }

        // Log in to obtain the member's access token.
        using var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = json.RootElement
            .GetProperty("accessToken")
            .GetString()!;

        // A regular Member cannot process payouts.
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{id}/cycles/{id}/payout");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Simulate a request from the frontend.
        request.Headers.Add(
            "Origin",
            "http://localhost:3000");

        // Act
        using var response = await _client.SendAsync(request);

        // Assert: The user is authenticated but lacks permission.
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        // The frontend must still be able to read the error.
        Assert.Equal(
            "http://localhost:3000",
            response.Headers.GetValues(
                "Access-Control-Allow-Origin").Single());

        // Authorization errors must use ProblemDetails.
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
}