using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RondiTrack.Telemetry;

public static class RondiTrackTelemetry
{
    // Custom tracing source for business operations.
    public static readonly ActivitySource ActivitySource =
        new("RondiTrack.Business");

    // Custom metrics source.
    public static readonly Meter Meter =
        new("RondiTrack.Business");

    // Count successfully recorded contributions.
    public static readonly Counter<long> ContributionsRecorded =
        Meter.CreateCounter<long>("ronditrack.contributions.recorded");

    // Count payouts by outcome.
    public static readonly Counter<long> PayoutsProcessed =
        Meter.CreateCounter<long>("ronditrack.payouts.processed");

    // Count authorization refusals by result.
    public static readonly Counter<long> AuthorizationRefusals =
        Meter.CreateCounter<long>("ronditrack.authorization.refusals");
}