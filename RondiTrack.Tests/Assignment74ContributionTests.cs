using System.Net;
using System.Net.Http.Json;

namespace RondiTrack.Tests;

public class Assignment74ContributionTests
{
    [Fact]
    public async Task RecordContribution_NewRequest_Returns201()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        // Create an active cycle for this test.
        var cycleResponse = await client.PostAsJsonAsync(
            "/api/v1/contributioncycles",
            new
            {
                id = 7401,
                stokvelId = 1,
                number = 741,
                targetAmount = 5000m
            });

        Assert.Equal(HttpStatusCode.Created, cycleResponse.StatusCode);

        // Record the contribution using integer cents.
        using var request = CreateContributionRequest(
            7401, "assignment74-new", 15050);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            "false",
            response.Headers.GetValues("Idempotency-Replayed").Single());
    }

    [Fact]
    public async Task RecordContribution_MissingIdempotencyKey_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        // Create an active contribution cycle.
        await CreateCycleAsync(client, 7405);

        // Submit a contribution without the required Idempotency-Key header.
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/stokvels/1/cycles/7405/contributions");

        request.Content = JsonContent.Create(new
        {
            userId = 1,
            amountInCents = 15050
        });

        var response = await client.SendAsync(request);

        // Missing idempotency keys must be rejected.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordContribution_SameKeyAndBody_ReturnsReplay()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        await CreateCycleAsync(client, 7402);

        // First submission creates the contribution.
        using var firstRequest = CreateContributionRequest(
            7402, "assignment74-replay", 15050);

        var firstResponse = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var firstBody = await firstResponse.Content.ReadAsStringAsync();

        // Reuse the same key and body to simulate a retry.
        using var secondRequest = CreateContributionRequest(
            7402, "assignment74-replay", 15050);

        var secondResponse = await client.SendAsync(secondRequest);
        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        Assert.Equal(firstBody, secondBody);
        Assert.Equal(
            "true",
            secondResponse.Headers.GetValues("Idempotency-Replayed").Single());
    }

    [Fact]
    public async Task RecordContribution_SameKeyDifferentAmount_Returns409()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        await CreateCycleAsync(client, 7403);

        // Record the original amount.
        using var firstRequest = CreateContributionRequest(
            7403, "assignment74-amount-conflict", 15050);

        var firstResponse = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Reusing the key with a different amount must fail.
        using var secondRequest = CreateContributionRequest(
            7403, "assignment74-amount-conflict", 20000);

        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task RecordContribution_NewKeyDuplicateContribution_Returns409()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        await CreateCycleAsync(client, 7404);

        // First key records the contribution.
        using var firstRequest = CreateContributionRequest(
            7404, "assignment74-first-key", 15050);

        var firstResponse = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // A different key cannot create a duplicate contribution.
        using var secondRequest = CreateContributionRequest(
            7404, "assignment74-second-key", 15050);

        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }


    [Fact]
    public async Task RecordContribution_RateLimit_Returns429WithRetryAfter()
    {
        // Allow only one financial request in this test.
        await using var factory = new CustomWebApplicationFactory
        {
            FinancialTokenLimit = 1
        };

        await factory.InitializeAsync();

        var client = factory.CreateClient();

        // Create a cycle before making the financial requests.
        await CreateCycleAsync(client, 7406);

        // The first contribution request should be accepted.
        using var firstRequest = CreateContributionRequest(
            7406, "assignment74-rate-limit-one", 15050);

        var firstResponse = await client.SendAsync(firstRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // The second financial request should exceed the token limit.
        using var secondRequest = CreateContributionRequest(
            7406, "assignment74-rate-limit-two", 15050);

        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            secondResponse.StatusCode);

        // The backend must tell the client how long to wait.
        Assert.True(
            secondResponse.Headers.TryGetValues(
                "Retry-After",
                out var retryAfterValues));

        var retryAfter = retryAfterValues.Single();

        Assert.True(int.TryParse(retryAfter, out var seconds));
        Assert.True(seconds > 0);
    }

    // Create a cycle using the existing API.
    private static async Task CreateCycleAsync(HttpClient client, int cycleId)
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

    // Build a request for Assignment 7.4's contribution endpoint.
    private static HttpRequestMessage CreateContributionRequest(
        int cycleId,
        string idempotencyKey,
        long amountInCents)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/1/cycles/{cycleId}/contributions");

        request.Headers.Add("Idempotency-Key", idempotencyKey);

        request.Content = JsonContent.Create(new
        {
            userId = 1,
            amountInCents
        });

        return request;
    }
}