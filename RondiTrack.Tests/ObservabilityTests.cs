using System.Diagnostics;
using System.Net;
using System.Text.Json;
using RondiTrack.Tests.TestSupport;

namespace RondiTrack.Tests;

public class ObservabilityTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ObservabilityTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    // Liveness must work anonymously.
    [Fact]
    public async Task Liveness_ReturnsHealthy_WithoutAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // Readiness must work anonymously when PostgreSQL is available.
    [Fact]
    public async Task Readiness_ReturnsHealthy_WithoutAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // A caller-supplied W3C trace ID must be preserved in a 401 response.
    [Fact]
    public async Task Unauthorized_PreservesIncomingTraceId()
    {
        using var client = _factory.CreateClient();

        const string traceId = "0123456789abcdef0123456789abcdef";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/users");

        request.Headers.TryAddWithoutValidation(
            "traceparent",
            $"00-{traceId}-0123456789abcdef-01");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            traceId,
            json.RootElement.GetProperty("correlationId").GetString());
    }
}