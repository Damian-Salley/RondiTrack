using Microsoft.EntityFrameworkCore;
using RondiTrack.Models;

namespace RondiTrack.Data.Seeding;

// Creates isolated demonstration records for Assignment 7.1.
// This method must only be called in Development when SeedDemoData is enabled.
public static class DevelopmentDataSeeder
{
    private const string DemoEmail = "assignment71-demo@ronditrack.example";

    public static async Task SeedAsync(RondiTrackDbContext db)
    {
        // Do not insert duplicate demonstration data.
        if (await db.Users.AnyAsync(u => u.NormalizedEmail ==
            DemoEmail.ToUpperInvariant()))
        {
            return;
        }

        // Use IDs above the existing maximum to avoid immediate conflicts
        // with records already stored in the database.
        var nextUserId = (await db.Users.MaxAsync(u => (int?)u.Id) ?? 0) + 1;
        var nextStokvelId =
            (await db.Stokvels.MaxAsync(s => (int?)s.Id) ?? 0) + 1;
        var nextCycleId =
            (await db.ContributionCycles.MaxAsync(c => (int?)c.Id) ?? 0) + 1;

        // Create the user who will belong to both stokvels.
        var demoUser = new User(
            nextUserId,
            "Demo",
            "Member",
            DemoEmail,
            "0000000000");

        // Create two stokvels with different contribution requirements.
        var savingsStokvel = new Stokvel(
            nextStokvelId,
            "Community Savings",
            250m);

        var familyStokvel = new Stokvel(
            nextStokvelId + 1,
            "Family Savings",
            100m);

        // Demonstrate different roles for the same user.
        savingsStokvel.AddMember(demoUser, "Treasurer");
        familyStokvel.AddMember(demoUser, "Member");

        // Create a second member to demonstrate contributions.
        var secondUser = new User(
            nextUserId + 1,
            "Sample",
            "Contributor",
            "assignment71-contributor@ronditrack.example",
            "0000000001");

        savingsStokvel.AddMember(secondUser, "Member");
        familyStokvel.AddMember(secondUser, "Member");

        // Use a fixed UTC payout date for predictable demonstrations.
        var payoutDate = new DateTime(
            2026, 11, 30, 22, 0, 0,
            DateTimeKind.Utc);

        var savingsCycle = new ContributionCycle(
            nextCycleId,
            savingsStokvel.Id,
            1,
            500m,
            payoutDate,
            250m);

        var familyCycle = new ContributionCycle(
            nextCycleId + 1,
            familyStokvel.Id,
            1,
            200m,
            payoutDate,
            100m);

        // This contribution is fully paid.
        // Johannesburg time is 1 November 2026 at 00:30.
        var paidContribution = new Contribution(
            demoUser.Id,
            savingsStokvel.Id,
            savingsCycle.Id,
            250m,
            new DateTime(
                2026, 10, 31, 22, 30, 0,
                DateTimeKind.Utc));

        // A ten-cent contribution demonstrates precise money formatting.
        var smallContribution = new Contribution(
            secondUser.Id,
            savingsStokvel.Id,
            savingsCycle.Id,
            0.10m,
            DateTime.UtcNow);

        // This contribution has been confirmed.
        smallContribution.Confirm();

        // Create a pending contribution without a payment timestamp.
        var pendingContribution = new Contribution(
            demoUser.Id,
            familyStokvel.Id,
            familyCycle.Id,
            100m);

        pendingContribution.MarkAsPending();

        // Use EF Core's configured retry strategy.
        // PostgreSQL retry support requires manually created transactions
        // to run inside this execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Begin a transaction inside the retry strategy.
            await using var transaction =
                await db.Database.BeginTransactionAsync();

            // Add all demonstration records.
            db.Users.AddRange(demoUser, secondUser);

            db.Stokvels.AddRange(
                savingsStokvel,
                familyStokvel);

            db.ContributionCycles.AddRange(
                savingsCycle,
                familyCycle);

            db.Contributions.AddRange(
                paidContribution,
                smallContribution,
                pendingContribution);

            // Save all records.
            await db.SaveChangesAsync();

            // Synchronize PostgreSQL's identity sequences.
            await db.Database.ExecuteSqlRawAsync("""
        SELECT setval(
            pg_get_serial_sequence('"Users"', 'Id'),
            (SELECT MAX("Id") FROM "Users")
        );
        """);

            await db.Database.ExecuteSqlRawAsync("""
        SELECT setval(
            pg_get_serial_sequence('"Stokvels"', 'Id'),
            (SELECT MAX("Id") FROM "Stokvels")
        );
        """);

            await db.Database.ExecuteSqlRawAsync("""
        SELECT setval(
            pg_get_serial_sequence('"ContributionCycles"', 'Id'),
            (SELECT MAX("Id") FROM "ContributionCycles")
        );
        """);

            // Commit only after every operation succeeds.
            await transaction.CommitAsync();
        });
    }
}