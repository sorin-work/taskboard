using System.Text.RegularExpressions;

namespace Taskboard.Api.Common;

/// <summary>
/// Gives every request an id, echoed in the X-Request-Id header, used as the
/// trace id in problem responses and attached to every log line. A sane
/// incoming X-Request-Id (from nginx or a client) is kept.
/// </summary>
public sealed partial class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
{
    public const string HeaderName = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var requestId = SafeId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
        {
            await next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex SafeId();
}
