using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Taskboard.Api.Data;

namespace Taskboard.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API against an in-memory SQLite database. One database per
/// factory instance, so each test class gets a clean slate.
/// </summary>
public class TaskboardApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>Tests sign up a lot of users from one "IP", so the limit is raised by default.</summary>
    protected virtual int AuthPermitsPerMinute => 10_000;

    public TaskboardApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Host=unused");
        builder.UseSetting("Jwt:Secret", "test-secret-that-is-long-enough-0123456789");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("RateLimiting:AuthPermitsPerMinute", AuthPermitsPerMinute.ToString(CultureInfo.InvariantCulture));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<TaskboardDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TaskboardDbContext>>();
            services.AddDbContext<TaskboardDbContext>(options => options
                .UseSqlite(_connection, sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .UseSnakeCaseNamingConvention());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        EnsureDatabase();
    }

    private bool _created;

    private void EnsureDatabase()
    {
        if (_created)
        {
            return;
        }

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TaskboardDbContext>().Database.EnsureCreated();
        _created = true;
    }

    public T WithDb<T>(Func<TaskboardDbContext, T> action)
    {
        using var scope = Services.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<TaskboardDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
