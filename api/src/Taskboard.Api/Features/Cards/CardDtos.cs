namespace Taskboard.Api.Features.Cards;

public sealed record CreateCardRequest(string Title);

public sealed record UpdateCardRequest(string Title, string? Description);

public sealed record CardSummaryDto(Guid Id, Guid ColumnId, string Title, long Position);

public sealed record CardUserDto(Guid Id, string DisplayName);

public sealed record CardDto(
    Guid Id,
    Guid BoardId,
    Guid ColumnId,
    string ColumnName,
    string Title,
    string Description,
    long Position,
    CardUserDto CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt);
