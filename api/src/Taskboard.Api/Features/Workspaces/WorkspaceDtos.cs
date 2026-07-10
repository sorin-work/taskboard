using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Workspaces;

public sealed record CreateWorkspaceRequest(string Name);

public sealed record UpdateWorkspaceRequest(string Name);

public sealed record WorkspaceDto(Guid Id, string Name, WorkspaceRole Role, int MemberCount, DateTime CreatedAt);

public sealed record MemberDto(Guid UserId, string Email, string DisplayName, WorkspaceRole Role, DateTime JoinedAt);

public sealed record UpdateMemberRequest(WorkspaceRole Role);

public sealed record WorkloadDto(Guid UserId, string DisplayName, int Open, int Overdue, int DueThisWeek);
