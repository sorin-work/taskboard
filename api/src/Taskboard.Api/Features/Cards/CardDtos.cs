namespace Taskboard.Api.Features.Cards;

public sealed record CreateCardRequest(string Title);

public sealed record UpdateCardRequest(string Title, string? Description);

/// <summary>Move a card into <paramref name="ColumnId"/>, right after <paramref name="AfterCardId"/> (or to the top).</summary>
public sealed record MoveCardRequest(Guid ColumnId, Guid? AfterCardId);

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
