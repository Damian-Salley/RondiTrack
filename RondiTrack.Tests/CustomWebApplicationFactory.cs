using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using RondiTrack.Data;
using RondiTrack.Tests.TestSupport;

namespace RondiTrack.Tests;

public class CustomWebApplicationFactory : PostgresApiFactory
{
    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();

        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Users"
                ("Id", "FirstName", "LastName", "Email", "NormalizedEmail", "PhoneNumber")
            VALUES
                (1, 'Damian', 'Salley', 'damian@example.com', 'DAMIAN@EXAMPLE.COM', '0821234567'),
                (2, 'Tim', 'Huang', 'tim@example.com', 'TIM@EXAMPLE.COM', '0831234567'),
                (3, 'Ryan', 'Smith', 'ryan@example.com', 'RYAN@EXAMPLE.COM', '0841234567');

            INSERT INTO "Stokvels"
                ("Id", "Name", "ContributionAmount")
            VALUES
                (1, 'Community Savings', 500);

            INSERT INTO "StokvelMembers"
                ("StokvelId", "UserId", "Role")
            VALUES
                (1, 1, 'Chairperson'),
                (1, 2, 'Member'),
                (1, 3, 'Member');
            """);
    }

    public new HttpClient CreateClient()
    {
        var client = base.CreateClient();

        using var scope = Services.CreateScope();

        var configuration = scope.ServiceProvider
            .GetRequiredService<IConfiguration>();

        var key = Convert.FromBase64String(
            configuration["Jwt:Key"]!);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "RondiTrack",
            audience: "RondiTrack",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "1")
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return client;
    }
}