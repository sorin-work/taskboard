using System.Threading.RateLimiting;

namespace Taskboard.Api.Common;

public static class RateLimiting
{
    /// <summary>Applied to sign in and sign up to slow down password guessing.</summary>
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddTaskboardRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:AuthPermitsPerMinute", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too many requests",
                        detail: "Too many attempts. Wait a minute and try again.")
                    .ExecuteAsync(context.HttpContext);
            };
        });

        return services;
    }
}
