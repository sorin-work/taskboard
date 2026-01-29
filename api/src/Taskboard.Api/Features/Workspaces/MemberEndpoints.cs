using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Workspaces;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}/members").WithTags("Members").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPatch("/{userId:guid}", UpdateRoleAsync).Validate<UpdateMemberRequest>();
        group.MapDelete("/{userId:guid}", RemoveAsync);

        return app;
    }

    private static async Task<List<MemberDto>> ListAsync(
        Guid workspaceId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireMemberAsync(workspaceId, principal.GetUserId(), ct: ct);
        var members = await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Select(m => new MemberDto(m.UserId, m.User.Email, m.User.DisplayName, m.Role, m.JoinedAt))
            .ToListAsync(ct);

        return [.. members.OrderByDescending(m => m.Role).ThenBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)];
    }

    private static async Task<MemberDto> UpdateRoleAsync(
        Guid workspaceId,
        Guid userId,
        UpdateMemberRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var actor = await access.RequireMemberAsync(workspaceId, principal.GetUserId(), WorkspaceRole.Admin, ct);
        var target = await FindMemberAsync(db, workspaceId, userId, ct);

        // Only owners can hand out or take away ownership.
        if ((target.Role == WorkspaceRole.Owner || request.Role == WorkspaceRole.Owner) && actor.Role != WorkspaceRole.Owner)
        {
            throw new ForbiddenException("Only workspace owners can change who owns the workspace.");
        }

        if (target.Role == WorkspaceRole.Owner && request.Role != WorkspaceRole.Owner)
        {
            await EnsureAnotherOwnerAsync(db, workspaceId, userId, ct);
        }

        target.Role = request.Role;
        await db.SaveChangesAsync(ct);

        return new MemberDto(target.UserId, target.User.Email, target.User.DisplayName, target.Role, target.JoinedAt);
    }

    private static async Task<IResult> RemoveAsync(
        Guid workspaceId,
        Guid userId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var actorId = principal.GetUserId();
        var actor = await access.RequireMemberAsync(workspaceId, actorId, ct: ct);
        var target = await FindMemberAsync(db, workspaceId, userId, ct);

        var leaving = actorId == userId;
        if (!leaving)
        {
            WorkspaceAccess.EnsureRole(actor, WorkspaceRole.Admin);
            if (target.Role == WorkspaceRole.Owner && actor.Role != WorkspaceRole.Owner)
            {
                throw new ForbiddenException("Only workspace owners can remove an owner.");
            }
        }

        if (target.Role == WorkspaceRole.Owner)
        {
            await EnsureAnotherOwnerAsync(db, workspaceId, userId, ct);
        }

        // Their cards stay, but they shouldn't show up as someone working on them.
        var assignments = await db.CardAssignees
            .Where(a => a.UserId == userId && a.Card.Board.WorkspaceId == workspaceId)
            .ToListAsync(ct);
        db.CardAssignees.RemoveRange(assignments);

        db.WorkspaceMembers.Remove(target);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<WorkspaceMember> FindMemberAsync(TaskboardDbContext db, Guid workspaceId, Guid userId, CancellationToken ct) =>
        await db.WorkspaceMembers
            .Include(m => m.User)
            .SingleOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct)
        ?? throw new NotFoundException("Member not found.");

    private static async Task EnsureAnotherOwnerAsync(TaskboardDbContext db, Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var otherOwners = await db.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspaceId && m.UserId != userId && m.Role == WorkspaceRole.Owner, ct);
        if (otherOwners == 0)
        {
            throw new ConflictException("A workspace needs at least one owner. Make someone else an owner first.");
        }
    }
}
