using Taskboard.Api.Features.Checklists;
using Taskboard.Api.Features.Labels;

namespace Taskboard.Api.Features.Cards;

public sealed record CreateCardRequest(string Title);

/// <summary>Replaces the editable fields of a card. A missing due date clears it.</summary>
public sealed record UpdateCardRequest(string Title, string? Description, DateOnly? DueDate = null, bool Completed = false);

/// <summary>Move a card into <paramref name="ColumnId"/>, right after <paramref name="AfterCardId"/> (or to the top).</summary>
public sealed record MoveCardRequest(Guid ColumnId, Guid? AfterCardId);

public sealed record SetCardLabelsRequest(IReadOnlyList<Guid> LabelIds);

public sealed record SetCardAssigneesRequest(IReadOnlyList<Guid> UserIds);

public sealed record CardSummaryDto(
    Guid Id,
    Guid ColumnId,
    string Title,
    long Position,
    DateOnly? DueDate,
    bool Completed,
    IReadOnlyList<Guid> AssigneeIds,
    IReadOnlyList<Guid> LabelIds,
    int ChecklistDone,
    int ChecklistTotal,
    int CommentCount);

public sealed record CardUserDto(Guid Id, string DisplayName);

public sealed record CardDto(
    Guid Id,
    Guid BoardId,
    Guid ColumnId,
    string ColumnName,
    string Title,
    string Description,
    long Position,
    DateOnly? DueDate,
    DateTime? CompletedAt,
    DateTime? ArchivedAt,
    CardUserDto CreatedBy,
    IReadOnlyList<CardUserDto> Assignees,
    IReadOnlyList<LabelDto> Labels,
    IReadOnlyList<ChecklistDto> Checklists,
    DateTime CreatedAt,
    DateTime UpdatedAt);
