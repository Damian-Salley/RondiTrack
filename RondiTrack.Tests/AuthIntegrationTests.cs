using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RondiTrack.Tests.TestSupport;

namespace RondiTrack.Tests;

[Collection("Postgres collection")]
public class AuthIntegrationTests
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(PostgresApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401ProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/stokvels");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnSameProblemDetails()
    {
        // Arrange
        var email = $"auth-{Guid.NewGuid():N}@example.com";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                id = Random.Shared.Next(100000, 2000000000),
                firstName = "Auth",
                lastName = "Tester",
                email,
                phoneNumber = "0821234567",
                password = "CorrectPassword123!"
            });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Act
        var wrongPassword = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password = "WrongPassword123!"
            });

        var unknownEmail = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = $"unknown-{Guid.NewGuid():N}@example.com",
                password = "WrongPassword123!"
            });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);

        using var wrongJson = JsonDocument.Parse(
            await wrongPassword.Content.ReadAsStringAsync());

        using var unknownJson = JsonDocument.Parse(
            await unknownEmail.Content.ReadAsStringAsync());

        Assert.Equal(
            wrongJson.RootElement.GetProperty("title").GetString(),
            unknownJson.RootElement.GetProperty("title").GetString());

        Assert.Equal(
            wrongJson.RootElement.GetProperty("status").GetInt32(),
            unknownJson.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesSuccessfully()
    {
        var client = _client;
        var email = $"rotate-{Guid.NewGuid():N}@example.com";
        var password = "CorrectPassword123!";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                id = Random.Shared.Next(100000, 2000000000),
                firstName = "Refresh",
                lastName = "Tester",
                email,
                phoneNumber = "0821234567",
                password
            });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var originalCookie = login.Headers.GetValues("Set-Cookie")
            .First(x => x.StartsWith("refreshToken="))
            .Split(';')[0];

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/refresh");

        request.Headers.Add("Cookie", originalCookie);

        var refresh = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var newCookie = refresh.Headers.GetValues("Set-Cookie")
            .First(x => x.StartsWith("refreshToken="))
            .Split(';')[0];

        Assert.NotEqual(originalCookie, newCookie);
    }

    [Fact]
    public async Task Refresh_ReusedToken_RevokesEntireFamily()
    {
        var client = _client;
        var email = $"replay-{Guid.NewGuid():N}@example.com";
        var password = "CorrectPassword123!";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                id = Random.Shared.Next(100000, 2000000000),
                firstName = "Replay",
                lastName = "Tester",
                email,
                phoneNumber = "0821234567",
                password
            });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var originalCookie = login.Headers.GetValues("Set-Cookie")
            .First(x => x.StartsWith("refreshToken="))
            .Split(';')[0];

        async Task<HttpResponseMessage> RefreshWithCookie(string cookie)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/auth/refresh");

            request.Headers.Add("Cookie", cookie);

            return await client.SendAsync(request);
        }

        // First use succeeds and rotates the token.
        var firstRefresh = await RefreshWithCookie(originalCookie);

        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        var newCookie = firstRefresh.Headers.GetValues("Set-Cookie")
            .First(x => x.StartsWith("refreshToken="))
            .Split(';')[0];

        // Reusing the old token must fail.
        var replay = await RefreshWithCookie(originalCookie);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // Replay must also revoke the newer token.
        var afterReplay = await RefreshWithCookie(newCookie);

        Assert.Equal(HttpStatusCode.Unauthorized, afterReplay.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var client = _client;
        var email = $"logout-{Guid.NewGuid():N}@example.com";
        var password = "CorrectPassword123!";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                id = Random.Shared.Next(100000, 2000000000),
                firstName = "Logout",
                lastName = "Tester",
                email,
                phoneNumber = "0821234567",
                password
            });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var cookie = login.Headers.GetValues("Set-Cookie")
            .First(x => x.StartsWith("refreshToken="))
            .Split(';')[0];

        using var logoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/logout");

        logoutRequest.Headers.Add("Cookie", cookie);

        var logout = await client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // The original token must no longer work.
        using var refreshRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/refresh");

        refreshRequest.Headers.Add("Cookie", cookie);

        var refresh = await client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}