using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Workspaces;

/// <summary>
/// Central place for "is this user allowed to touch this workspace" checks.
/// Non-members get a 404 so we don't reveal which workspaces exist.
/// </summary>
public sealed class WorkspaceAccess(TaskboardDbContext db)
{
    public async Task<WorkspaceMember> RequireMemberAsync(
        Guid workspaceId,
        Guid userId,
        WorkspaceRole minimum = WorkspaceRole.Member,
        CancellationToken ct = default)
    {
        var member = await db.WorkspaceMembers
            .SingleOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct)
            ?? throw new NotFoundException("Workspace not found.");

        EnsureRole(member, minimum);
        return member;
    }

    public static void EnsureRole(WorkspaceMember member, WorkspaceRole minimum)
    {
        if (member.Role < minimum)
        {
            throw new ForbiddenException(minimum switch
            {
                WorkspaceRole.Owner => "Only workspace owners can do that.",
                _ => "Only workspace owners and admins can do that.",
            });
        }
    }
}
