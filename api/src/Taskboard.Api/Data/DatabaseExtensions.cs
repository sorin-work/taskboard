using Microsoft.EntityFrameworkCore;

namespace Taskboard.Api.Data;

public static class DatabaseExtensions
{
    public static IServiceCollection AddTaskboardDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<TaskboardDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseSnakeCaseNamingConvention());

        return services;
    }

    /// <summary>Applies pending migrations when Database:MigrateOnStartup is true.</summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:MigrateOnStartup", false))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskboardDbContext>();
        await db.Database.MigrateAsync();
    }
}
