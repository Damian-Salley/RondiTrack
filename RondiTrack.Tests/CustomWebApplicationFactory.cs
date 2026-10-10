using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
                ("Id", "FirstName", "LastName", "Email", "PhoneNumber")
            VALUES
                (1, 'Damian', 'Salley', 'damian@example.com', '0821234567'),
                (2, 'Tim', 'Huang', 'tim@example.com', '0831234567'),
                (3, 'Ryan', 'Smith', 'ryan@example.com', '0841234567');

            INSERT INTO "Stokvels"
                ("Id", "Name", "ContributionAmount")
            VALUES
                (1, 'Community Savings', 500);

            INSERT INTO "StokvelMembers"
                ("StokvelId", "UserId")
            VALUES
                (1, 1),
                (1, 2),
                (1, 3);
            """);
    }
}