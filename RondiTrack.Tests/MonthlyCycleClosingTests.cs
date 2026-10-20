using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RondiTrack.BackgroundServices;
using RondiTrack.Data;
using RondiTrack.Models;
using RondiTrack.Tests.TestSupport;

namespace RondiTrack.Tests;

// Integration tests for the monthly contribution-cycle closing service.
// Each test uses an isolated PostgreSQL Testcontainer.
public class MonthlyCycleClosingTests
{
    // Creates a new database and adds a stokvel with three members.
    private static async Task<PostgresApiFactory> CreateFactoryAsync(
        int stokvelId,
        int firstUserId)
    {
        var factory = new PostgresApiFactory();

        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        // Create three users with unique IDs and email addresses.
        var users = Enumerable.Range(0, 3)
            .Select(index => new User(
                firstUserId + index,
                $"Member{index + 1}",
                "Test",
                $"member{firstUserId + index}@example.com",
                $"082000{firstUserId + index:D4}"))
            .ToList();

        db.Users.AddRange(users);

        // Each member must contribute R500 per cycle.
        var stokvel = new Stokvel(
            stokvelId,
            "Monthly Closing Test",
            500m);

        // Add all three users as stokvel members.
        foreach (var user in users)
        {
            stokvel.AddMember(user);
        }

        db.Stokvels.Add(stokvel);

        await db.SaveChangesAsync();

        return factory;
    }

    // Creates an expired cycle whose deadline has already passed.
    private static async Task AddExpiredCycleAsync(
        PostgresApiFactory factory,
        int stokvelId,
        int cycleId,
        int cycleNumber)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        // Yesterday's date ensures the cycle is expired.
        var expiredDate = DateTime.UtcNow.AddDays(-1);

        var cycle = new ContributionCycle(
            cycleId,
            stokvelId,
            cycleNumber,
            1500m,
            expiredDate,
            500m);

        db.ContributionCycles.Add(cycle);

        await db.SaveChangesAsync();
    }

    // Runs the actual monthly closing logic against PostgreSQL.
    private static async Task RunClosingAsync(
        PostgresApiFactory factory)
    {
        var service = new MonthlyCycleClosingService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            factory.Services.GetRequiredService<TimeProvider>(),
            NullLogger<MonthlyCycleClosingService>.Instance);

        await service.CloseExpiredCyclesAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task ExpiredCycle_IsClosed()
    {
        await using var factory =
            await CreateFactoryAsync(8101, 8110);

        await AddExpiredCycleAsync(factory, 8101, 8120, 1);

        // Execute the monthly closing service.
        await RunClosingAsync(factory);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var cycle = await db.ContributionCycles
            .SingleAsync(c => c.Id == 8120);

        // Verify the cycle is closed and has a closing timestamp.
        Assert.Equal("Closed", cycle.Status);
        Assert.NotNull(cycle.ClosedAtUtc);
    }

    [Fact]
    public async Task ClosingCycle_CalculatesCorrectArrearsInCents()
    {
        await using var factory =
            await CreateFactoryAsync(8201, 8210);

        await AddExpiredCycleAsync(factory, 8201, 8220, 1);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            // Member 1 pays R500: no arrears.
            db.Contributions.Add(new Contribution(
                8210, 8201, 8220, 500m));

            // Member 2 pays R300: R200 outstanding.
            db.Contributions.Add(new Contribution(
                8211, 8201, 8220, 300m));

            // Member 3 pays nothing: R500 outstanding.
            await db.SaveChangesAsync();
        }

        await RunClosingAsync(factory);

        using var verificationScope =
            factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var arrears = await verificationDb.CycleArrears
            .Where(a => a.ContributionCycleId == 8220)
            .ToListAsync();

        // Fully paid members must not receive arrears records.
        Assert.Equal(2, arrears.Count);

        Assert.DoesNotContain(
            arrears,
            a => a.UserId == 8210);

        // R200 = 20,000 cents.
        Assert.Contains(
            arrears,
            a => a.UserId == 8211 &&
                 a.AmountCents == 20000);

        // R500 = 50,000 cents.
        Assert.Contains(
            arrears,
            a => a.UserId == 8212 &&
                 a.AmountCents == 50000);

        Assert.Equal(
            70000L,
            arrears.Sum(a => a.AmountCents));
    }

    [Fact]
    public async Task ClosingSameCycleTwice_DoesNotDuplicateArrears()
    {
        await using var factory =
            await CreateFactoryAsync(8301, 8310);

        await AddExpiredCycleAsync(factory, 8301, 8320, 1);

        // Execute closing twice.
        await RunClosingAsync(factory);
        await RunClosingAsync(factory);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var arrears = await db.CycleArrears
            .Where(a => a.ContributionCycleId == 8320)
            .ToListAsync();

        // Three unpaid members should produce exactly three records.
        Assert.Equal(3, arrears.Count);

        Assert.All(
            arrears,
            a => Assert.Equal(50000L, a.AmountCents));
    }

    [Fact]
    public async Task MultipleExpiredCycles_AreAllClosed()
    {
        await using var factory =
            await CreateFactoryAsync(8401, 8410);

        // Simulate two missed cycles after application downtime.
        await AddExpiredCycleAsync(factory, 8401, 8420, 1);
        await AddExpiredCycleAsync(factory, 8401, 8421, 2);

        await RunClosingAsync(factory);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var cycles = await db.ContributionCycles
            .Where(c => c.StokvelId == 8401)
            .ToListAsync();

        Assert.Equal(2, cycles.Count);

        // Both expired cycles must be closed.
        Assert.All(
            cycles,
            cycle =>
            {
                Assert.Equal("Closed", cycle.Status);
                Assert.NotNull(cycle.ClosedAtUtc);
            });

        // Three unpaid members across two cycles = six arrears.
        var arrearsCount = await db.CycleArrears
            .CountAsync(a =>
                a.ContributionCycleId == 8420 ||
                a.ContributionCycleId == 8421);

        Assert.Equal(6, arrearsCount);
    }

    [Fact]
    public async Task ConcurrentClosing_DoesNotCreateDuplicateArrears()
    {
        await using var factory =
            await CreateFactoryAsync(8501, 8510);

        await AddExpiredCycleAsync(factory, 8501, 8520, 1);

        // Simulate two workers trying to close the same cycle.
        await Task.WhenAll(
            RunClosingAsync(factory),
            RunClosingAsync(factory));

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var arrears = await db.CycleArrears
            .Where(a => a.ContributionCycleId == 8520)
            .ToListAsync();

        // The database lock should prevent duplicate processing.
        Assert.Equal(3, arrears.Count);

        Assert.Equal(
            3,
            arrears.Select(a => a.UserId).Distinct().Count());

        var cycle = await db.ContributionCycles
            .SingleAsync(c => c.Id == 8520);

        Assert.Equal("Closed", cycle.Status);
    }

    [Fact]
    public async Task ClosingExpiredCycle_DoesNotCloseFutureCycle()
    {
        await using var factory =
            await CreateFactoryAsync(8601, 8610);

        await AddExpiredCycleAsync(factory, 8601, 8620, 1);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            // This cycle expires next month and must remain active.
            var futureCycle = new ContributionCycle(
                8621,
                8601,
                2,
                1500m,
                DateTime.UtcNow.AddMonths(1),
                500m);

            db.ContributionCycles.Add(futureCycle);

            await db.SaveChangesAsync();
        }

        await RunClosingAsync(factory);

        using var verificationScope =
            factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var expiredCycle = await verificationDb.ContributionCycles
            .SingleAsync(c => c.Id == 8620);

        var futureCycleAfterClosing =
            await verificationDb.ContributionCycles
                .SingleAsync(c => c.Id == 8621);

        // Only the expired cycle should be closed.
        Assert.Equal("Closed", expiredCycle.Status);
        Assert.Equal("Active", futureCycleAfterClosing.Status);

        Assert.Null(futureCycleAfterClosing.ClosedAtUtc);

        // The future cycle must not have any arrears records.
        Assert.False(
            await verificationDb.CycleArrears.AnyAsync(
                a => a.ContributionCycleId == 8621));
    }
}