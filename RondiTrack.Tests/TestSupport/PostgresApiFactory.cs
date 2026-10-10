using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RondiTrack.Data;
using Testcontainers.PostgreSql;

namespace RondiTrack.Tests.TestSupport;

public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
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
        builder.UseSetting(
            "ConnectionStrings:RondiTrack",
            _container.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }
}