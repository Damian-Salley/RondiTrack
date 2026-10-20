using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RondiTrack.Data;
using RondiTrack.Models;
// Provides access to the database transaction used by PostgreSQL commands.
using Microsoft.EntityFrameworkCore.Storage;

namespace RondiTrack.BackgroundServices;

// Background service responsible for automatically closing expired contribution cycles.
public class MonthlyCycleClosingService : BackgroundService
{
    // Creates dependency injection scopes so we can access the database safely.
    private readonly IServiceScopeFactory _scopeFactory;

    // Provides the current time and allows us to control time during testing.
    private readonly TimeProvider _timeProvider;

    // Records information and errors while the background service runs.
    private readonly ILogger<MonthlyCycleClosingService> _logger;

    // Constructor: receives the services needed to perform monthly cycle closing.
    public MonthlyCycleClosingService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<MonthlyCycleClosingService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Runs when the application starts and checks for expired cycles every hour.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check immediately on startup to catch cycles missed while the API was offline.
        await TryCloseExpiredCyclesAsync(stoppingToken);

        // Use TimeProvider so the timer can be controlled in automated tests.
        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(1),
            _timeProvider);

        try
        {
            // Continue checking every hour until the application shuts down.
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await TryCloseExpiredCyclesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Cancellation is expected when the application shuts down.
        }
    }

    // Allows integration tests to trigger cycle closing directly.
    // The background service still calls this method automatically in production.
    public async Task TryCloseExpiredCyclesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CloseExpiredCyclesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Allow normal application shutdown.
        }
        catch (Exception ex)
        {
            // Log the error so the service can try again on its next scheduled check.
            _logger.LogError(
                ex,
                "An error occurred while closing expired contribution cycles.");
        }
    }

    // Public so integration tests can execute the monthly closing logic directly.
    // The background service will continue running automatically in production.
    public async Task CloseExpiredCyclesAsync(CancellationToken cancellationToken)
    {
        // Create a new dependency injection scope for database access.
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        // EF Core's execution strategy retries temporary database failures.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Use a fresh DbContext for each retry attempt.
            // This avoids reusing tracked changes from a failed transaction.
            using var retryScope = _scopeFactory.CreateScope();

            var retryDb = retryScope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            // Start a transaction so all changes succeed or fail together.
            await using var transaction =
                await retryDb.Database.BeginTransactionAsync(cancellationToken);

            // Obtain a PostgreSQL transaction-level advisory lock.
            // Other service instances using this lock must wait until it is released.
            //
            // ExecuteScalarAsync is used because PostgreSQL's advisory-lock
            // function returns a value, rather than an affected-row count.
            var connection = retryDb.Database.GetDbConnection();

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction.GetDbTransaction();
                command.CommandText = "SELECT pg_advisory_xact_lock(64001)";

                await command.ExecuteScalarAsync(cancellationToken);
            }

            // Get the current UTC time from the injected TimeProvider.
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            // Find contribution cycles whose closing dates have passed.
            // Already-closed cycles are excluded to prevent duplicate processing.
            var expiredCycles = await retryDb.ContributionCycles
                .Where(c => c.ClosedAtUtc == null && c.EndsAtUtc <= now)
                .ToListAsync(cancellationToken);

            foreach (var cycle in expiredCycles)
            {
                // Find every member belonging to this stokvel.
                var members = await retryDb.StokvelMembers
                    .Where(m => m.StokvelId == cycle.StokvelId)
                    .ToListAsync(cancellationToken);

                // Find contributions recorded for the current cycle.
                var contributions = await retryDb.Contributions
                    .Where(c => c.StokvelId == cycle.StokvelId &&
                                c.Cycle == cycle.Id)
                    .ToListAsync(cancellationToken);

                foreach (var member in members)
                {
                    // Calculate the amount this member has already contributed.
                    var paid = contributions
                        .Where(c => c.UserId == member.UserId)
                        .Sum(c => c.ContributionAmount);

                    // Use the per-member contribution amount saved when the cycle was created.
                    // This prevents future changes to the stokvel's contribution amount
                    // from affecting historical arrears calculations.
                    var outstanding = Math.Max(
                        0m,
                        cycle.RequiredContributionAmount - paid);

                    // Convert the outstanding amount from rands to integer cents.
                    var amountCents = checked((long)(outstanding * 100m));

                    // Only create an arrears record when the member owes money.
                    if (amountCents > 0)
                    {
                        retryDb.CycleArrears.Add(new CycleArrears(
                            cycle.Id,
                            member.UserId,
                            amountCents));
                    }
                }

                // Mark the cycle as closed using the injected current time.
                cycle.Close(now);
            }

            // Save the arrears records and cycle statuses together.
            await retryDb.SaveChangesAsync(cancellationToken);

            // Commit the transaction only after all database changes succeed.
            await transaction.CommitAsync(cancellationToken);

            // Record how many cycles were processed successfully.
            _logger.LogInformation(
                "Monthly cycle closing completed. Processed {Count} cycles.",
                expiredCycles.Count);
        });
    }
}