using Taskboard.Api.Domain;
using Taskboard.Api.Features.Labels;

namespace Taskboard.Api.Features.Boards;

public sealed record CreateBoardRequest(string Name, string? Description, IReadOnlyList<string>? Columns);

public sealed record CopyBoardRequest(string Name, bool IncludeCards);

public sealed record UpdateBoardRequest(string Name, string? Description, bool Archived);

public sealed record BoardSummaryDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string Description,
    int CardCount,
    DateTime CreatedAt,
    DateTime? ArchivedAt,
    bool Starred);

public sealed record ColumnDto(Guid Id, string Name, int Position, int? WipLimit)
{
    public static ColumnDto From(BoardColumn column) => new(column.Id, column.Name, column.Position, column.WipLimit);
}

public sealed record BoardMemberDto(Guid UserId, string DisplayName, string Email);

public sealed record BoardDto(
    Guid Id,
    Guid WorkspaceId,
    string WorkspaceName,
    string Name,
    string Description,
    WorkspaceRole Role,
    DateTime CreatedAt,
    DateTime? ArchivedAt,
    bool Starred,
    IReadOnlyList<ColumnDto> Columns,
    IReadOnlyList<LabelDto> Labels,
    IReadOnlyList<BoardMemberDto> Members);

public sealed record CreateColumnRequest(string Name);

public sealed record UpdateColumnRequest(string Name);

public sealed record MoveColumnRequest(int Position);

public sealed record SetWipLimitRequest(int? Limit);
