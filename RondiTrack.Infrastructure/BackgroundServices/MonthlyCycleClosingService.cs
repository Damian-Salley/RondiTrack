using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RondiTrack.Data;
using RondiTrack.Models;

namespace RondiTrack.BackgroundServices;

// Automatically closes expired contribution cycles and records arrears.
public class MonthlyCycleClosingService : BackgroundService
{
    // Creates dependency injection scopes for database operations.
    private readonly IServiceScopeFactory _scopeFactory;

    // Provides the current time and supports controlled testing.
    private readonly TimeProvider _timeProvider;

    // Records background processing information and failures.
    private readonly ILogger<MonthlyCycleClosingService> _logger;

    // Receive the dependencies needed by the background service.
    public MonthlyCycleClosingService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<MonthlyCycleClosingService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Start immediately and repeat the closing check every hour.
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Process cycles missed while the application was offline.
        await TryCloseExpiredCyclesAsync(stoppingToken);

        // Use TimeProvider so tests can control timer behaviour.
        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(1),
            _timeProvider);

        try
        {
            // Continue checking until the application shuts down.
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await TryCloseExpiredCyclesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Application shutdown is expected, not an error.
        }
    }

    // Run the closing operation and log unexpected failures.
    // Public so integration tests can trigger the operation.
    public async Task TryCloseExpiredCyclesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await CloseExpiredCyclesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Allow normal application shutdown.
        }
        catch (Exception ex)
        {
            // Log the failure so it can be investigated.
            // The next scheduled run can retry the operation.
            _logger.LogError(
                ex,
                "An error occurred while closing expired contribution cycles.");
        }
    }

    // Find expired active cycles, calculate arrears and close them.
    public async Task CloseExpiredCyclesAsync(
        CancellationToken cancellationToken)
    {
        // Create a scope to obtain the EF Core execution strategy.
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        // Retry the operation when EF Core detects a transient failure.
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Each retry receives a fresh DbContext.
            // This avoids reusing tracked changes from a failed attempt.
            using var retryScope = _scopeFactory.CreateScope();

            var retryDb = retryScope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            // Start a transaction so arrears and cycle status
            // are committed together.
            await using var transaction =
                await retryDb.Database.BeginTransactionAsync(
                    cancellationToken);

            // Obtain the shared PostgreSQL advisory lock.
            //
            // Other operations using this same lock must wait,
            // preventing conflicting changes during closing.
            var connection = retryDb.Database.GetDbConnection();

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction.GetDbTransaction();

                command.CommandText =
                    "SELECT pg_advisory_xact_lock(64001)";

                // Advisory lock functions return a value,
                // so ExecuteScalarAsync is appropriate.
                await command.ExecuteScalarAsync(cancellationToken);
            }

            // Obtain the current UTC time.
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            // Select only active cycles whose end dates have passed.
            //
            // Exclude Paid cycles and cycles already closed.
            // This prevents unnecessary or duplicate arrears processing.
            var expiredCycles = await retryDb.ContributionCycles
                .Where(c =>
                    c.Status == "Active" &&
                    c.ClosedAtUtc == null &&
                    c.EndsAtUtc <= now)
                .ToListAsync(cancellationToken);

            foreach (var cycle in expiredCycles)
            {
                // Retrieve all members belonging to the stokvel.
                var members = await retryDb.StokvelMembers
                    .Where(m => m.StokvelId == cycle.StokvelId)
                    .ToListAsync(cancellationToken);

                // Retrieve contributions recorded for this cycle.
                var contributions = await retryDb.Contributions
                    .Where(c =>
                        c.StokvelId == cycle.StokvelId &&
                        c.Cycle == cycle.Id)
                    .ToListAsync(cancellationToken);

                // Calculate outstanding contributions for each member.
                foreach (var member in members)
                {
                    // Reuse the same calculation as manual cycle closing.
                    var amountCents = CycleClosing.OutstandingCents(
                        cycle,
                        member.UserId,
                        contributions);

                    // Only create arrears when the member owes money.
                    if (amountCents > 0)
                    {
                        retryDb.CycleArrears.Add(
                            new CycleArrears(
                                cycle.Id,
                                member.UserId,
                                amountCents));
                    }
                }

                // Mark the cycle as closed at the current UTC time.
                cycle.Close(now);
            }

            // Save arrears records and cycle statuses together.
            await retryDb.SaveChangesAsync(cancellationToken);

            // Commit the transaction only after successful saving.
            await transaction.CommitAsync(cancellationToken);

            // Log the number of cycles processed.
            _logger.LogInformation(
                "Monthly cycle closing completed. Processed {Count} cycles.",
                expiredCycles.Count);
        });
    }
}