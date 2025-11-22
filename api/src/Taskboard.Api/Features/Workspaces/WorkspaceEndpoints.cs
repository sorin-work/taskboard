using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Workspaces;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces").WithTags("Workspaces").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync).Validate<CreateWorkspaceRequest>();
        group.MapGet("/{workspaceId:guid}", GetAsync);
        group.MapPatch("/{workspaceId:guid}", UpdateAsync).Validate<UpdateWorkspaceRequest>();
        group.MapDelete("/{workspaceId:guid}", DeleteAsync);

        return app;
    }

    private static async Task<List<WorkspaceDto>> ListAsync(ClaimsPrincipal principal, TaskboardDbContext db, CancellationToken ct)
    {
        var userId = principal.GetUserId();
        return await db.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Workspace.Name)
            .Select(m => new WorkspaceDto(
                m.WorkspaceId,
                m.Workspace.Name,
                m.Role,
                m.Workspace.Members.Count,
                m.Workspace.CreatedAt))
            .ToListAsync(ct);
    }

    private static async Task<IResult> CreateAsync(
        CreateWorkspaceRequest request,
        ClaimsPrincipal principal,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var now = clock.GetUtcNow().UtcDateTime;
        var workspace = new Workspace { Id = Guid.NewGuid(), Name = request.Name.Trim(), CreatedAt = now };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner, JoinedAt = now });

        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(ct);

        var dto = new WorkspaceDto(workspace.Id, workspace.Name, WorkspaceRole.Owner, 1, workspace.CreatedAt);
        return Results.Created($"/api/workspaces/{workspace.Id}", dto);
    }

    private static async Task<WorkspaceDto> GetAsync(
        Guid workspaceId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var member = await access.RequireMemberAsync(workspaceId, principal.GetUserId(), ct: ct);
        return await ToDtoAsync(db, workspaceId, member.Role, ct);
    }

    private static async Task<WorkspaceDto> UpdateAsync(
        Guid workspaceId,
        UpdateWorkspaceRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var member = await access.RequireMemberAsync(workspaceId, principal.GetUserId(), WorkspaceRole.Admin, ct);
        var workspace = await db.Workspaces.SingleAsync(w => w.Id == workspaceId, ct);
        workspace.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(db, workspaceId, member.Role, ct);
    }

    private static async Task<IResult> DeleteAsync(
        Guid workspaceId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireMemberAsync(workspaceId, principal.GetUserId(), WorkspaceRole.Owner, ct);
        var workspace = await db.Workspaces.SingleAsync(w => w.Id == workspaceId, ct);
        db.Workspaces.Remove(workspace);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<WorkspaceDto> ToDtoAsync(TaskboardDbContext db, Guid workspaceId, WorkspaceRole role, CancellationToken ct)
    {
        return await db.Workspaces
            .Where(w => w.Id == workspaceId)
            .Select(w => new WorkspaceDto(w.Id, w.Name, role, w.Members.Count, w.CreatedAt))
            .SingleAsync(ct);
    }
}
