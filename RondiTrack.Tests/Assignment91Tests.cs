using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RondiTrack.Data;
using RondiTrack.Models;
using RondiTrack.Services;

namespace RondiTrack.Tests;

public class Assignment91Tests
{
    private static async Task<CustomWebApplicationFactory> Setup()
    {
        var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE "StokvelMembers" SET "Role" = 'Treasurer' WHERE "StokvelId" = 1 AND "UserId" = 1;
            """);
        db.Users.Add(new User(4, "Invitee", "Test", "invitee@example.com", "0821234567"));
        db.Users.Add(new User(5, "Outsider", "Test", "outsider@example.com", "0821234567"));
        var other = new Stokvel(2, "Other stokvel", 500m);
        db.Stokvels.Add(other);
        db.StokvelMembers.AddRange(new StokvelMember(2, 1, "Member", DateTime.UtcNow),
            new StokvelMember(2, 5, "Treasurer", DateTime.UtcNow));
        db.ContributionCycles.Add(new ContributionCycle(91, 1, 1, 1500m, DateTime.UtcNow.AddDays(10), 500m));
        db.ContributionCycles.Add(new ContributionCycle(92, 2, 1, 1000m, DateTime.UtcNow.AddDays(10), 500m));
        db.Contributions.Add(new Contribution(2, 1, 91, 200m, DateTime.UtcNow));
        db.Contributions.Add(new Contribution(5, 2, 92, 200m, DateTime.UtcNow));
        await db.SaveChangesAsync();
        return factory;
    }
    private static HttpClient Client(CustomWebApplicationFactory factory, int userId)
    {
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var token = new JwtSecurityToken("RondiTrack", "RondiTrack",
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
    private static MultipartFormDataContent File(byte[] bytes, string name = "proof.pdf", string type = "application/pdf")
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(type);
        form.Add(content, "file", name);
        return form;
    }
    private const string Proof = "/api/v1/stokvels/1/cycles/91/contributions/2/proof";
    private const string Preview = "/api/v1/contributioncycles/91/close-preview";
    private static async Task<HttpResponseMessage> Close(HttpClient client, string version, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/contributioncycles/91/close")
            { Content = JsonContent.Create(new { version }) };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Invites_ArePrivate_AndConcurrentJoinIsIdempotent()
    {
        await using var factory = await Setup();
        var treasurer = Client(factory, 1);
        var created = await treasurer.PostAsJsonAsync("/api/v1/stokvels/1/invites", new { rules = "Pay monthly", schedule = "Monthly" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var invite = (await created.Content.ReadFromJsonAsync<JoinInvite>())!;
        var invitee = Client(factory, 4);
        var response = await invitee.GetAsync($"/api/v1/invites/{invite.Code}");
        var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(new[] { "alreadyMember", "contributionAmountCents", "memberCount", "name", "rules", "schedule", "stokvelId" },
            json.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(50000, json.GetProperty("contributionAmountCents").GetInt64());
        Assert.False(json.GetProperty("alreadyMember").GetBoolean());
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => invitee.PostAsync($"/api/v1/invites/{invite.Code}/join", null)));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var preview = await invitee.GetFromJsonAsync<InvitePreview>($"/api/v1/invites/{invite.Code}");
        Assert.True(preview!.AlreadyMember);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>().StokvelMembers.CountAsync(m => m.StokvelId == 1 && m.UserId == 4));
    }

    [Fact]
    public async Task Invites_UnknownExpiredAndWrongRoleHaveDistinctResponses()
    {
        await using var factory = await Setup();
        var member = Client(factory, 2);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync("/api/v1/stokvels/1/invites", new { rules = "Rules", schedule = "Monthly" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync("/api/v1/invites/unknown")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
        db.JoinInvites.Add(new JoinInvite { Code = "expired", StokvelId = 1, Rules = "Rules", Schedule = "Monthly", ExpiresAtUtc = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Gone, (await member.GetAsync("/api/v1/invites/expired")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await member.PostAsync("/api/v1/invites/expired/join", null)).StatusCode);
    }

    [Fact]
    public async Task Proof_OnlyUploaderAndLocalTreasurerCanDownload_AndHeadersArePrivate()
    {
        await using var factory = await Setup();
        var uploader = Client(factory, 2);
        Assert.Equal(HttpStatusCode.OK, (await uploader.PostAsync(Proof, File("%PDF-1.7\nproof"u8.ToArray()))).StatusCode);
        foreach (var id in new[] { 1, 2 })
        {
            var response = await Client(factory, id).GetAsync(Proof);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("private", response.Headers.CacheControl!.ToString());
            Assert.Contains("no-store", response.Headers.CacheControl.ToString());
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        }
        foreach (var id in new[] { 3, 5 })
            Assert.Equal(HttpStatusCode.NotFound, (await Client(factory, id).GetAsync(Proof)).StatusCode);
        // User 1 is treasurer only in stokvel 1; that role gives no access in stokvel 2.
        var otherProof = "/api/v1/stokvels/2/cycles/92/contributions/5/proof";
        Assert.Equal(HttpStatusCode.OK, (await Client(factory, 5).PostAsync(otherProof, File("%PDF-1.7\nother"u8.ToArray()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client(factory, 1).GetAsync(otherProof)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client(factory, 1).PostAsync(Proof, File("%PDF-1.7"u8.ToArray()))).StatusCode);
    }

    [Fact]
    public async Task Proof_RejectsRenamedTextAndOversize_AcceptsJustUnderLimit_ThenLocks()
    {
        await using var factory = await Setup();
        var uploader = Client(factory, 2);
        Assert.Equal(HttpStatusCode.BadRequest, (await uploader.PostAsync(Proof, File("this is text"u8.ToArray()))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await uploader.PostAsync(Proof, File(new byte[JoinPaymentClosingService.ProofLimit + 1]))).StatusCode);
        var bytes = new byte[JoinPaymentClosingService.ProofLimit - 1];
        "%PDF-1.7"u8.CopyTo(bytes);
        Assert.Equal(HttpStatusCode.OK, (await uploader.PostAsync(Proof, File(bytes, "anything.txt", "text/plain"))).StatusCode);
        var treasurer = Client(factory, 1);
        var preview = (await treasurer.GetFromJsonAsync<ClosePreview>(Preview))!;
        Assert.Equal(HttpStatusCode.OK, (await Close(treasurer, preview.Version, "lock-proof")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await uploader.PostAsync(Proof, File(bytes))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await uploader.DeleteAsync(Proof)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await uploader.GetAsync(Proof)).StatusCode);
    }

    [Fact]
    public async Task Close_StalePreviewRefused_NewPreviewConfirmed_ConcurrentRetriesRecordOnce()
    {
        await using var factory = await Setup();
        var treasurer = Client(factory, 1);
        var before = (await treasurer.GetFromJsonAsync<ClosePreview>(Preview))!;
        Assert.Equal(130000, before.TotalCents);
        Assert.Equal(3, before.Members.Count);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/stokvels/1/cycles/91/contributions")
            { Content = JsonContent.Create(new { userId = 3, amountInCents = 50000 }) };
        request.Headers.Add("Idempotency-Key", "new-payment");
        Assert.Equal(HttpStatusCode.Created, (await treasurer.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Close(treasurer, before.Version, "stale")).StatusCode);
        using (var scope = factory.Services.CreateScope())
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>().CycleArrears.ToListAsync());
        var after = (await treasurer.GetFromJsonAsync<ClosePreview>(Preview))!;
        Assert.NotEqual(before.Version, after.Version);
        Assert.Equal(80000, after.TotalCents);
        var closes = await Task.WhenAll(Close(treasurer, after.Version, "same-close"), Close(treasurer, after.Version, "same-close"));
        Assert.All(closes, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(await closes[0].Content.ReadAsStringAsync(), await closes[1].Content.ReadAsStringAsync());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
            Assert.Equal(after.TotalCents, await db.CycleArrears.SumAsync(a => a.AmountCents));
            Assert.Equal(2, await db.CycleArrears.CountAsync());
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Close(treasurer, before.Version, "same-close")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Close(treasurer, after.Version, "different-close")).StatusCode);
    }

    [Fact]
    public async Task Close_RequiresLocalTreasurerAndVersionAndKey()
    {
        await using var factory = await Setup();
        var member = Client(factory, 2);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync(Preview)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Close(member, "version", "key")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(factory, 5).GetAsync(Preview)).StatusCode);
        var treasurer = Client(factory, 1);
        Assert.Equal(HttpStatusCode.BadRequest, (await treasurer.PostAsJsonAsync("/api/v1/contributioncycles/91/close", new { version = "test" })).StatusCode);
    }

    [Theory]
    [InlineData("%PDF-1.7", "application/pdf")]
    [InlineData("fake.pdf", null)]
    public void ProofType_UsesBytes(string value, string? expected) =>
        Assert.Equal(expected, JoinPaymentClosingService.ProofType(System.Text.Encoding.UTF8.GetBytes(value)));
}
