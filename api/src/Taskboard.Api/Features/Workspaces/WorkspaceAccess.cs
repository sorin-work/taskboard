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
        var member = await FindMemberAsync(workspaceId, userId, ct)
            ?? throw new NotFoundException("Workspace not found.");

        EnsureRole(member, minimum);
        return member;
    }

    public async Task<(Board Board, WorkspaceMember Member)> RequireBoardAsync(
        Guid boardId,
        Guid userId,
        WorkspaceRole minimum = WorkspaceRole.Member,
        CancellationToken ct = default)
    {
        var board = await db.Boards.SingleOrDefaultAsync(b => b.Id == boardId, ct);
        var member = board is null ? null : await FindMemberAsync(board.WorkspaceId, userId, ct);
        if (board is null || member is null)
        {
            throw new NotFoundException("Board not found.");
        }

        EnsureRole(member, minimum);
        return (board, member);
    }

    public async Task<(Card Card, WorkspaceMember Member)> RequireCardAsync(
        Guid cardId,
        Guid userId,
        WorkspaceRole minimum = WorkspaceRole.Member,
        CancellationToken ct = default)
    {
        var card = await db.Cards.Include(c => c.Board).SingleOrDefaultAsync(c => c.Id == cardId, ct);
        var member = card is null ? null : await FindMemberAsync(card.Board.WorkspaceId, userId, ct);
        if (card is null || member is null)
        {
            throw new NotFoundException("Card not found.");
        }

        EnsureRole(member, minimum);
        return (card, member);
    }

    public async Task<(BoardColumn Column, WorkspaceMember Member)> RequireColumnAsync(Guid columnId, Guid userId, CancellationToken ct = default)
    {
        var column = await db.Columns.Include(c => c.Board).SingleOrDefaultAsync(c => c.Id == columnId, ct);
        var member = column is null ? null : await FindMemberAsync(column.Board.WorkspaceId, userId, ct);
        if (column is null || member is null)
        {
            throw new NotFoundException("Column not found.");
        }

        return (column, member);
    }

    public async Task<(Label Label, WorkspaceMember Member)> RequireLabelAsync(Guid labelId, Guid userId, CancellationToken ct = default)
    {
        var label = await db.Labels.Include(l => l.Board).SingleOrDefaultAsync(l => l.Id == labelId, ct);
        var member = label is null ? null : await FindMemberAsync(label.Board.WorkspaceId, userId, ct);
        if (label is null || member is null)
        {
            throw new NotFoundException("Label not found.");
        }

        return (label, member);
    }

    private Task<WorkspaceMember?> FindMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct) =>
        db.WorkspaceMembers.SingleOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

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
