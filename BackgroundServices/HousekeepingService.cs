using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RondiTrack.Data;
using RondiTrack.Idempotency;

namespace RondiTrack.BackgroundServices;

public class HousekeepingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<HousekeepingService> _logger;

    public HousekeepingService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IIdempotencyStore idempotencyStore,
        ILogger<HousekeepingService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Perform housekeeping immediately when the API starts.
        await RunHousekeepingAsync(stoppingToken);

        // Repeat housekeeping every hour using the injected clock.
        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(1),
            _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunHousekeepingAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown: no error needs to be logged.
        }
    }

    private async Task RunHousekeepingAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            // Create a scope because DbContext is a scoped service.
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            // Read the current time from TimeProvider for testability.
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            // Delete expired refresh tokens directly in PostgreSQL.
            // ExecuteDeleteAsync performs the deletion without loading
            // each token into application memory.
            var deletedTokens = await db.RefreshTokens
                .Where(token => token.ExpiresAt <= now)
                .ExecuteDeleteAsync(cancellationToken);

            // Remove expired idempotency records from memory.
            var deletedRecords =
                await _idempotencyStore.RemoveExpiredAsync();

            _logger.LogInformation(
                "Housekeeping completed. Removed {TokenCount} expired refresh tokens and {RecordCount} expired idempotency records.",
                deletedTokens,
                deletedRecords);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation during shutdown is expected.
        }
        catch (Exception ex)
        {
            // Log failures so housekeeping can retry on its next run.
            _logger.LogError(
                ex,
                "An error occurred during housekeeping.");
        }
    }
}