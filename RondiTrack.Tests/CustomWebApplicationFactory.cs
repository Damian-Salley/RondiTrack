using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RondiTrack.Data;

namespace RondiTrack.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Database.EnsureDeleted();
            db.Database.Migrate();

            db.Database.ExecuteSqlRaw("""
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
        });
    }
}