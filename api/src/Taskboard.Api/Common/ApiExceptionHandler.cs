using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Taskboard.Api.Common;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;
        if (exception is ApiException apiException)
        {
            problem = new ProblemDetails
            {
                Status = apiException.StatusCode,
                Title = apiException.Title,
                Detail = apiException.Message,
            };
        }
        else if (exception is BadHttpRequestException badRequest)
        {
            // Malformed json, wrong types, missing body: the client's fault, so say what's wrong.
            problem = new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Bad request",
                Detail = badRequest.InnerException is System.Text.Json.JsonException
                    ? "The request body isn't valid JSON for this endpoint."
                    : badRequest.Message,
            };
        }
        else
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Something went wrong",
                Detail = "An unexpected error occurred. Please try again.",
            };
        }

        httpContext.Response.StatusCode = problem.Status.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
