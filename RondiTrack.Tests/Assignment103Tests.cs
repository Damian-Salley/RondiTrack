using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RondiTrack.Data;
using RondiTrack.Data.Seeding;
using RondiTrack.Models;
using RondiTrack.Tests.TestSupport;

namespace RondiTrack.Tests;

public class Assignment103Tests
{
    [Fact]
    public async Task PayoutOrder_MembershipOrderNextPaidEmptyAndContract()
    {
        await using var factory = new PostgresApiFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
        await DevelopmentDataSeeder.SeedAsync(db);
        var stokvel = await db.Stokvels.SingleAsync(s => s.Name == "Community Savings");
        var cycle = await db.ContributionCycles.SingleAsync(c =>
            c.StokvelId == stokvel.Id && c.Status == "Active");
        var memberIds = await db.StokvelMembers.Where(m => m.StokvelId == stokvel.Id)
            .OrderBy(m => m.UserId).Select(m => m.UserId).ToListAsync();
        var newcomer = await db.Users.SingleAsync(u => u.FirstName == "New");
        var treasurer = await db.Users.SingleAsync(u => u.FirstName == "Demo");
        var member = await db.Users.SingleAsync(u => u.FirstName == "Sample");
        using var client = factory.CreateClient();
        var route = $"/api/v1/stokvels/{stokvel.Id}/cycles/{cycle.Id}/payouts";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
        SignIn(client, factory, newcomer.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route)).StatusCode);
        SignIn(client, factory, member.Id);
        var order = (await client.GetFromJsonAsync<JsonElement[]>(route))!;
        Assert.Equal(memberIds, order!.Select(row => row.GetProperty("userId").GetInt32()));
        Assert.Equal(new[] { 1, 2 }, order.Select(row => row.GetProperty("position").GetInt32()));
        Assert.Equal(new[] { "Next", "Waiting" }, order.Select(row => row.GetProperty("status").GetString()));
        Assert.All(order, row => Assert.Equal(50000, row.GetProperty("amountCents").GetInt64()));
        Assert.All(order, row => Assert.Equal(JsonValueKind.Null, row.GetProperty("paidAtUtc").ValueKind));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(
            $"/api/v1/stokvels/{stokvel.Id}/cycles/2147483647/payouts")).StatusCode);
        var otherCycle = await db.ContributionCycles.FirstAsync(c => c.StokvelId != stokvel.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(
            $"/api/v1/stokvels/{stokvel.Id}/cycles/{otherCycle.Id}/payouts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync(route[..^1], null)).StatusCode);
        SignIn(client, factory, treasurer.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(route[..^1], null)).StatusCode);
        order = (await client.GetFromJsonAsync<JsonElement[]>(route))!;
        Assert.Equal(new[] { "Paid", "Waiting" }, order!.Select(row => row.GetProperty("status").GetString()));
        Assert.Equal(JsonValueKind.String, order[0].GetProperty("paidAtUtc").ValueKind);

        // No order exists for a cycle with no members (service boundary).
        var empty = new Stokvel(900001, "Empty order", 100m);
        db.Stokvels.Add(empty);
        db.ContributionCycles.Add(new ContributionCycle(900001, empty.Id, 1, 0m));
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<RondiTrack.Services.IRondiTrackService>();
        Assert.Empty(await service.GetPayoutOrderAsync(empty.Id, 900001));

        var json = await client.GetStringAsync("/openapi/v1.json");
        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty(
            "/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/payouts", out _));
        var activityParameters = document.RootElement.GetProperty("paths")
            .GetProperty("/api/v1/me/activity").GetProperty("get").GetProperty("parameters");
        var pageSize = activityParameters.EnumerateArray().Single(
            parameter => parameter.GetProperty("name").GetString() == "pageSize");
        Assert.Equal("integer", pageSize.GetProperty("schema").GetProperty("type").GetString());
        var export = Environment.GetEnvironmentVariable("RONDITRACK_CONTRACT_EXPORT");
        if (!string.IsNullOrEmpty(export)) await File.WriteAllTextAsync(export, json);
    }

    private static void SignIn(HttpClient client, PostgresApiFactory factory, int userId)
    {
        using var scope = factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var token = new JwtSecurityToken("RondiTrack", "RondiTrack",
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(config["Jwt:Key"]!)),
                SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}

