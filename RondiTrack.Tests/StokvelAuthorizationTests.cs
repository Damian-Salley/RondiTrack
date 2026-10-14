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
public class StokvelAuthorizationTests
{
    private readonly PostgresApiFactory _factory;

    public StokvelAuthorizationTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Payout_NonMember404_Member403_TreasurerAuthorized()
    {
        var id = Random.Shared.Next(100000, 1000000000);

        var nonMemberId = id;
        var memberId = id + 1;
        var treasurerId = id + 2;
        var stokvelId = id;
        var cycleId = id;

        var password = "CorrectPassword123!";

        using var client = _factory.CreateClient();

        // Register three users.
        async Task Register(int userId, string email)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/auth/register",
                new
                {
                    id = userId,
                    firstName = "Test",
                    lastName = "User",
                    email,
                    phoneNumber = "0821234567",
                    password
                });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var nonMemberEmail = $"nonmember-{id}@example.com";
        var memberEmail = $"member-{id}@example.com";
        var treasurerEmail = $"treasurer-{id}@example.com";

        await Register(nonMemberId, nonMemberEmail);
        await Register(memberId, memberEmail);
        await Register(treasurerId, treasurerEmail);

        // Seed a stokvel and its memberships directly in the test database.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();

            var stokvel = new Stokvel(stokvelId, "Authorization Test", 500m);

            db.Stokvels.Add(stokvel);

            db.StokvelMembers.Add(new StokvelMember(
                stokvelId,
                memberId,
                "Member",
                DateTime.UtcNow));

            db.StokvelMembers.Add(new StokvelMember(
                stokvelId,
                treasurerId,
                "Treasurer",
                DateTime.UtcNow));

            db.ContributionCycles.Add(new ContributionCycle(
                cycleId,
                stokvelId,
                1,
                500m));

            await db.SaveChangesAsync();
        }

        // Log in and retrieve each user's access token.
        async Task<string> Login(string email)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email, password });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            return json.RootElement.GetProperty("accessToken").GetString()!;
        }

        var nonMemberToken = await Login(nonMemberEmail);
        var memberToken = await Login(memberEmail);
        var treasurerToken = await Login(treasurerEmail);

        // Call the same protected payout endpoint with each token.
        async Task<HttpResponseMessage> RequestPayout(string token)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/payout");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return await client.SendAsync(request);
        }

        // A non-member must receive 404 with the caller's W3C trace ID.
        const string traceId = "abcdef0123456789abcdef0123456789";

        using var nonMemberRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/payout");

        // Authenticate as the non-member.
        nonMemberRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", nonMemberToken);

        // Supply a valid W3C traceparent header.
        nonMemberRequest.Headers.TryAddWithoutValidation(
            "traceparent",
            $"00-{traceId}-0123456789abcdef-01");

        using var nonMemberResponse =
            await client.SendAsync(nonMemberRequest);

        // Verify the authorization response.
        Assert.Equal(HttpStatusCode.NotFound, nonMemberResponse.StatusCode);

        // Read the ProblemDetails response.
        using var nonMemberJson = JsonDocument.Parse(
            await nonMemberResponse.Content.ReadAsStringAsync());

        // The response must preserve the incoming trace ID.
        Assert.Equal(
            traceId,
            nonMemberJson.RootElement
                .GetProperty("correlationId")
                .GetString());
        // A Member belongs to the stokvel but cannot process payouts.
        var memberResponse = await RequestPayout(memberToken);

        Assert.Equal(HttpStatusCode.Forbidden, memberResponse.StatusCode);

        // A Treasurer is allowed past authorization.
        // A missing contribution cycle may still cause a business-rule error.
        var treasurerResponse = await RequestPayout(treasurerToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, treasurerResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, treasurerResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotFound, treasurerResponse.StatusCode);
    }

    [Fact]
    public async Task Contribution_MemberCanRecordOwn_ButNotOthers()
    {
        var id = Random.Shared.Next(100000, 1000000000);

        var memberId = id;
        var otherMemberId = id + 1;
        var stokvelId = id;
        var cycleId = id;

        const string password = "CorrectPassword123!";
        var email = $"contribution-member-{id}@example.com";

        using var client = _factory.CreateClient();

        // Register the member who will make the requests.
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                id = memberId,
                firstName = "Test",
                lastName = "Member",
                email,
                phoneNumber = "0821234567",
                password
            });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Register another member.
        var otherRegisterResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                id = otherMemberId,
                firstName = "Other",
                lastName = "Member",
                email = $"contribution-other-{id}@example.com",
                phoneNumber = "0821234568",
                password
            });

        Assert.Equal(HttpStatusCode.Created, otherRegisterResponse.StatusCode);

        // Create the stokvel, memberships and contribution cycle.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Stokvels.Add(new Stokvel(
                stokvelId,
                "Contribution Authorization Test",
                500m));

            db.StokvelMembers.Add(new StokvelMember(
                stokvelId,
                memberId,
                "Member",
                DateTime.UtcNow));

            db.StokvelMembers.Add(new StokvelMember(
                stokvelId,
                otherMemberId,
                "Member",
                DateTime.UtcNow));

            db.ContributionCycles.Add(new ContributionCycle(
                cycleId,
                stokvelId,
                1,
                1000m));

            await db.SaveChangesAsync();
        }

        // Log in as the regular Member.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = json.RootElement
            .GetProperty("accessToken")
            .GetString()!;

        // Helper to submit a contribution using the Member's JWT.
        async Task<HttpResponseMessage> RecordContribution(
            int targetUserId,
            string idempotencyKey)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/stokvels/{stokvelId}/members/{targetUserId}/contributions");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Headers.Add("Idempotency-Key", idempotencyKey);

            request.Content = JsonContent.Create(new
            {
                cycle = cycleId
            });

            return await client.SendAsync(request);
        }

        // A regular Member can record their own contribution.
        using var ownResponse = await RecordContribution(
            memberId,
            $"own-{id}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);

        // A regular Member cannot record another member's contribution.
        using var otherResponse = await RecordContribution(
            otherMemberId,
            $"other-{id}");

        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }
}