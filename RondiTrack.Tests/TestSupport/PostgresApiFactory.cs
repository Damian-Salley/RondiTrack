using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RondiTrack.Data;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace RondiTrack.Tests.TestSupport;

public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public CapturedLoggerProvider CapturedLogs { get; } = new();
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17")
            .WithDatabase("ronditrack_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use the PostgreSQL Testcontainer instead of the development database.
        builder.UseSetting(
            "ConnectionStrings:RondiTrack",
            _container.GetConnectionString());

        // Generate a separate JWT signing key for each test factory.
        // No real signing secrets are stored in the test project.
        builder.UseSetting(
            "Jwt:Key",
            Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));

        builder.ConfigureLogging(logging =>
{
    logging.AddProvider(CapturedLogs);
});
    }

    public new async Task DisposeAsync()
    {
        // Export pending traces and metrics before shutting down.
        using (var scope = Services.CreateScope())
        {
            var tracerProvider = scope.ServiceProvider
                .GetRequiredService<TracerProvider>();

            tracerProvider.ForceFlush();
        }

        await base.DisposeAsync();
        await _container.DisposeAsync();
    }
}