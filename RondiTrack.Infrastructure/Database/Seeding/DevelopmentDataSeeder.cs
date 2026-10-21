
using Microsoft.EntityFrameworkCore;
using RondiTrack.Models;

namespace RondiTrack.Data.Seeding;

// Creates demonstration records for Assignments 7.1 and 7.2.
// Only called in Development when SeedDemoData is enabled.
public static class DevelopmentDataSeeder
{
    private const string DemoEmail = "assignment71-demo@ronditrack.example";
    private const string ContributorEmail =
        "assignment71-contributor@ronditrack.example";

    public static async Task SeedAsync(RondiTrackDbContext db)
    {
        // Preserve existing Assignment 7.1 demo data.
        if (await db.Users.AnyAsync(u =>
            u.NormalizedEmail == DemoEmail.ToUpperInvariant()))
        {
            await SeedAssignment72Async(db);
            return;
        }

        // Generate IDs above the current database maximum.
        var nextUserId =
            (await db.Users.MaxAsync(u => (int?)u.Id) ?? 0) + 1;

        var nextStokvelId =
            (await db.Stokvels.MaxAsync(s => (int?)s.Id) ?? 0) + 1;

        var nextCycleId =
            (await db.ContributionCycles.MaxAsync(c => (int?)c.Id) ?? 0) + 1;

        // Create demonstration users.
        var demoUser = new User(
            nextUserId,
            "Demo",
            "Member",
            DemoEmail,
            "0000000000");

        var secondUser = new User(
            nextUserId + 1,
            "Sample",
            "Contributor",
            ContributorEmail,
            "0000000001");

        // Create two stokvels.
        var savingsStokvel = new Stokvel(
            nextStokvelId,
            "Community Savings",
            250m);

        var familyStokvel = new Stokvel(
            nextStokvelId + 1,
            "Family Savings",
            100m);

        // Demonstrate Treasurer and Member roles.
        savingsStokvel.AddMember(demoUser, "Treasurer");
        familyStokvel.AddMember(demoUser, "Member");

        savingsStokvel.AddMember(secondUser, "Member");
        familyStokvel.AddMember(secondUser, "Member");

        // Fixed UTC payout date.
        var payoutDate = new DateTime(
            2026, 11, 30, 22, 0, 0,
            DateTimeKind.Utc);

        // Existing Assignment 7.1 cycles.
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

        // Fully paid contribution.
        var paidContribution = new Contribution(
            demoUser.Id,
            savingsStokvel.Id,
            savingsCycle.Id,
            250m,
            new DateTime(
                2026, 10, 31, 22, 30, 0,
                DateTimeKind.Utc));

        // Small confirmed contribution for money formatting.
        var smallContribution = new Contribution(
            secondUser.Id,
            savingsStokvel.Id,
            savingsCycle.Id,
            0.10m,
            DateTime.UtcNow);

        smallContribution.Confirm();

        // Pending contribution.
        var pendingContribution = new Contribution(
            demoUser.Id,
            familyStokvel.Id,
            familyCycle.Id,
            100m);

        pendingContribution.MarkAsPending();

        // Use EF Core's configured retry strategy.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync();

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

            await db.SaveChangesAsync();

            // Synchronize PostgreSQL identity sequences.
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

            await transaction.CommitAsync();
        });

        // Add Assignment 7.2 records after initial seeding.
        await SeedAssignment72Async(db);
    }

    private static async Task SeedAssignment72Async(
        RondiTrackDbContext db)
    {
        // Find the existing demonstration stokvel and member.
        var stokvel = await db.Stokvels
            .FirstOrDefaultAsync(s => s.Name == "Community Savings");

        var member = await db.Users
            .FirstOrDefaultAsync(u =>
                u.NormalizedEmail == ContributorEmail.ToUpperInvariant());

        if (stokvel is null || member is null)
            return;

        // Retrieve existing cycles to prevent duplicates.
        var cycles = await db.ContributionCycles
            .Where(c => c.StokvelId == stokvel.Id)
            .ToListAsync();

        var nextCycleId =
            (await db.ContributionCycles.MaxAsync(c => (int?)c.Id) ?? 0) + 1;

        // Cycle 2: Closed.
        var closedCycle = cycles.FirstOrDefault(c => c.Number == 2);

        if (closedCycle is null)
        {
            closedCycle = new ContributionCycle(
                nextCycleId++,
                stokvel.Id,
                2,
                500m,
                new DateTime(
                    2026, 9, 30, 22, 0, 0,
                    DateTimeKind.Utc),
                250m);

            closedCycle.Close(
                new DateTime(
                    2026, 9, 30, 22, 0, 0,
                    DateTimeKind.Utc));

            db.ContributionCycles.Add(closedCycle);
        }

        // Cycle 3: Paid out.
        if (!cycles.Any(c => c.Number == 3))
        {
            var paidCycle = new ContributionCycle(
                nextCycleId,
                stokvel.Id,
                3,
                500m,
                new DateTime(
                    2026, 8, 31, 22, 0, 0,
                    DateTimeKind.Utc),
                250m);

            paidCycle.MarkAsPaid();

            db.ContributionCycles.Add(paidCycle);
        }

        // Add R250.00 arrears for the member in the closed cycle.
        var hasArrears = await db.CycleArrears.AnyAsync(a =>
            a.ContributionCycleId == closedCycle.Id &&
            a.UserId == member.Id);

        if (!hasArrears)
        {
            db.CycleArrears.Add(new CycleArrears(
                closedCycle.Id,
                member.Id,
                25000));
        }

        // Save the new Assignment 7.2 records.
        await db.SaveChangesAsync();

        // Keep PostgreSQL's cycle identity sequence synchronized.
        await db.Database.ExecuteSqlRawAsync("""
            SELECT setval(
                pg_get_serial_sequence('"ContributionCycles"', 'Id'),
                (SELECT MAX("Id") FROM "ContributionCycles")
            );
            """);
    }
}