using Taskboard.Api.Data;

namespace Taskboard.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", CheckAsync).WithTags("Health");
        return app;
    }

    /// <summary>200 when the api can reach the database, 503 otherwise.</summary>
    private static async Task<IResult> CheckAsync(TaskboardDbContext db, CancellationToken ct)
    {
        var databaseUp = await db.Database.CanConnectAsync(ct);
        return databaseUp
            ? Results.Ok(new { status = "ok", database = "ok" })
            : Results.Json(new { status = "degraded", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
