using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using RondiTrack.BackgroundServices;
using RondiTrack.Data;
using Testcontainers.PostgreSql;

namespace RondiTrack.Tests.TestSupport;

// Creates an isolated ASP.NET Core application and PostgreSQL database
// for integration tests.
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Captures application logs so tests can inspect them.
    public CapturedLoggerProvider CapturedLogs { get; } = new();

    // Each factory uses its own PostgreSQL Docker container.
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17")
            .WithDatabase("ronditrack_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async Task InitializeAsync()
    {
        // Start the isolated PostgreSQL database.
        await _container.StartAsync();

        // Accessing Services starts the test application.
        // The background services have been disabled in ConfigureWebHost.
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        // Create all database tables before integration tests execute.
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Connect to the isolated PostgreSQL Testcontainer.
        builder.UseSetting(
            "ConnectionStrings:RondiTrack",
            _container.GetConnectionString());

        // Generate a unique JWT signing key for the test application.
        builder.UseSetting(
            "Jwt:Key",
            Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));

        // Allow sufficient requests for normal integration tests.
        builder.UseSetting(
            "RateLimiting:Login:PermitLimit",
            "1000");

        builder.UseSetting(
            "RateLimiting:Financial:TokenLimit",
            "1000");

        builder.UseSetting(
            "RateLimiting:Financial:TokensPerPeriod",
            "1000");

        // Customize the application's services for integration testing.
        builder.ConfigureServices(services =>
        {
            // Disable automatic cycle closing in ordinary API tests.
            // We will test cycle closing separately under controlled conditions.
            services.RemoveAll<MonthlyCycleClosingService>();

            // Disable automatic housekeeping in ordinary API tests.
            // This prevents cleanup from running before migrations finish.
            services.RemoveAll<HousekeepingService>();

            // Remove hosted-service registrations by their implementation type.
            // AddHostedService<T>() registers services as IHostedService,
            // so RemoveAll<T>() alone would not remove those registrations.
            var backgroundServiceRegistrations = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    (descriptor.ImplementationType ==
                        typeof(MonthlyCycleClosingService) ||
                     descriptor.ImplementationType ==
                        typeof(HousekeepingService)))
                .ToList();

            foreach (var registration in backgroundServiceRegistrations)
            {
                services.Remove(registration);
            }
        });

        // Capture application logs during integration tests.
        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(CapturedLogs);
        });
    }

    public new async Task DisposeAsync()
    {
        // Export pending telemetry before stopping the application.
        using (var scope = Services.CreateScope())
        {
            var tracerProvider = scope.ServiceProvider
                .GetRequiredService<TracerProvider>();

            tracerProvider.ForceFlush();
        }

        // Dispose of the test application.
        await base.DisposeAsync();

        // Remove the isolated PostgreSQL container.
        await _container.DisposeAsync();
    }
}