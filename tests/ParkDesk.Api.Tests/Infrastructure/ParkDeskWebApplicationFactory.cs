using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkDesk.Api.Data;
using ParkDesk.Api.Services;

namespace ParkDesk.Api.Tests.Infrastructure;

/// <summary>
/// Spins up the API in-memory against a SQLite "shared cache" database so controller tests run
/// through the real ASP.NET Core pipeline (auth, model binding, ProblemDetails) without a real
/// SQL Server instance. The keep-alive connection stops the in-memory database from being dropped
/// while the per-request DbContexts open and close their own connections to the same database,
/// which is what lets the optimistic-concurrency tests exercise genuinely independent connections.
/// </summary>
public class ParkDeskWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"DataSource=file:{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private SqliteConnection? _keepAliveConnection;

    public string ConnectionString => _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connectionString));

            _keepAliveConnection = new SqliteConnection(_connectionString);
            _keepAliveConnection.Open();

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            DbSeeder.SeedDataAsync(db, hasher).GetAwaiter().GetResult();
        });
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connectionString).Options;
        return new AppDbContext(options);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _keepAliveConnection?.Dispose();
        base.Dispose(disposing);
    }
}
