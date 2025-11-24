using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Boards;

public sealed record CreateBoardRequest(string Name, string? Description, IReadOnlyList<string>? Columns);

public sealed record UpdateBoardRequest(string Name, string? Description, bool Archived);

public sealed record BoardSummaryDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string Description,
    int CardCount,
    DateTime CreatedAt,
    DateTime? ArchivedAt);

public sealed record ColumnDto(Guid Id, string Name, int Position);

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
    IReadOnlyList<ColumnDto> Columns,
    IReadOnlyList<BoardMemberDto> Members);
