namespace Taskboard.Api.Common;

/// <summary>
/// An error that maps directly onto an HTTP problem response. Thrown from
/// endpoints and services, turned into problem details by <see cref="ApiExceptionHandler"/>.
/// </summary>
public abstract class ApiException(int statusCode, string title, string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;

    public string Title { get; } = title;
}

public sealed class NotFoundException(string detail)
    : ApiException(StatusCodes.Status404NotFound, "Not found", detail);

public sealed class ForbiddenException(string detail)
    : ApiException(StatusCodes.Status403Forbidden, "Forbidden", detail);

public sealed class ConflictException(string detail)
    : ApiException(StatusCodes.Status409Conflict, "Conflict", detail);

public sealed class BadRequestException(string detail)
    : ApiException(StatusCodes.Status400BadRequest, "Bad request", detail);

public sealed class UnauthorizedException(string detail)
    : ApiException(StatusCodes.Status401Unauthorized, "Unauthorized", detail);
