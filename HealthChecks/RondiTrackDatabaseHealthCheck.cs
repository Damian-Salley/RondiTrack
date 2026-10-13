using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RondiTrack.Data;

namespace RondiTrack.HealthChecks;

public class RondiTrackDatabaseHealthCheck : IHealthCheck
{
    private readonly RondiTrackDbContext _db;

    public RondiTrackDatabaseHealthCheck(RondiTrackDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connected = await _db.Database.CanConnectAsync(
            cancellationToken);

        return connected
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
    }
}