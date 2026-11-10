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

public class Assignment101Tests
{
    [Fact]
    public async Task Seed_IsIdempotent_HasThreePersonasAndScopedRoles()
    {
        await using var factory = new PostgresApiFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
        await DevelopmentDataSeeder.SeedAsync(db);
        await DevelopmentDataSeeder.SeedAsync(db);
        var users = await db.Users.ToListAsync();
        Assert.Equal(3, users.Count);
        var demo = users.Single(u => u.Email == "assignment71-demo@ronditrack.example");
        var newcomer = users.Single(u => u.Email == "assignment101-new@ronditrack.example");
        var roles = await db.StokvelMembers.Where(m => m.UserId == demo.Id)
            .OrderBy(m => m.StokvelId).Select(m => m.Role).ToListAsync();
        Assert.Equal(new[] { "Treasurer", "Member" }, roles);
        Assert.False(await db.StokvelMembers.AnyAsync(m => m.UserId == newcomer.Id));
    }

    [Fact]
    public async Task Confirmation_EnforcesMembershipRoleStatusAndPersists()
    {
        await using var factory = new PostgresApiFactory();
        await factory.InitializeAsync();
        int stokvelId, cycleId, memberId, treasurerId, newcomerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
            await DevelopmentDataSeeder.SeedAsync(db);
            stokvelId = (await db.Stokvels.SingleAsync(s => s.Name == "Community Savings")).Id;
            cycleId = (await db.ContributionCycles.SingleAsync(c => c.StokvelId == stokvelId && c.Status == "Active")).Id;
            treasurerId = (await db.Users.SingleAsync(u => u.FirstName == "Demo")).Id;
            memberId = (await db.Users.SingleAsync(u => u.FirstName == "Sample")).Id;
            newcomerId = (await db.Users.SingleAsync(u => u.FirstName == "New")).Id;
        }
        var client = factory.CreateClient();
        var route = $"/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/contributions/{treasurerId}/confirm";
        SignIn(client, factory, newcomerId);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync(route, null)).StatusCode);
        SignIn(client, factory, memberId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync(route, null)).StatusCode);
        SignIn(client, factory, treasurerId);
        var response = await client.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", row.GetProperty("status").GetString());
        Assert.Equal(25000, row.GetProperty("amountCents").GetInt64());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(route, null)).StatusCode);
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
        Assert.Equal("Confirmed", (await verifyDb.Contributions.FindAsync(stokvelId, treasurerId, cycleId))!.Status);
        var closed = await verifyDb.ContributionCycles.SingleAsync(c => c.StokvelId == stokvelId && c.Status == "Closed");
        verifyDb.Contributions.Add(new Contribution(treasurerId, stokvelId, closed.Id, 250m));
        await verifyDb.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(
            $"/api/v1/stokvels/{stokvelId}/cycles/{closed.Id}/contributions/{treasurerId}/confirm", null)).StatusCode);
    }

    [Fact]
    public async Task OpenApi_ExportsCurrentCentsAndDetailContract()
    {
        await using var factory = new PostgresApiFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");
        using var doc = JsonDocument.Parse(json);
        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.GetProperty("HomeStokvelResponse").GetProperty("properties").TryGetProperty("arrearsCents", out _));
        var cycle = schemas.GetProperty("ContributionCycleResponse").GetProperty("properties");
        Assert.True(cycle.TryGetProperty("status", out _));
        Assert.True(cycle.TryGetProperty("dueDateUtc", out _));
        Assert.True(cycle.TryGetProperty("requiredContributionCents", out _));
        Assert.True(schemas.GetProperty("ContributionResponse").GetProperty("properties").TryGetProperty("amountCents", out _));
        // Optional export is generated by the real application, never hand-written JSON.
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
