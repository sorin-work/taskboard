using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Labels;

public static class LabelEndpoints
{
    public static IEndpointRouteBuilder MapLabelEndpoints(this IEndpointRouteBuilder app)
    {
        var board = app.MapGroup("/api/boards/{boardId:guid}/labels").WithTags("Labels").RequireAuthorization();
        board.MapGet("/", ListAsync);
        board.MapPost("/", CreateAsync).Validate<LabelRequest>();

        var label = app.MapGroup("/api/labels/{labelId:guid}").WithTags("Labels").RequireAuthorization();
        label.MapPatch("/", UpdateAsync).Validate<LabelRequest>();
        label.MapDelete("/", DeleteAsync);

        return app;
    }

    private static async Task<List<LabelDto>> ListAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireBoardAsync(boardId, principal.GetUserId(), ct: ct);
        return await ForBoardAsync(db, boardId, ct);
    }

    private static async Task<IResult> CreateAsync(
        Guid boardId,
        LabelRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireBoardAsync(boardId, principal.GetUserId(), ct: ct);
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(db, boardId, name, null, ct);

        var label = new Label { Id = Guid.NewGuid(), BoardId = boardId, Name = name, Color = request.Color };
        db.Labels.Add(label);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/labels/{label.Id}", new LabelDto(label.Id, label.Name, label.Color));
    }

    private static async Task<LabelDto> UpdateAsync(
        Guid labelId,
        LabelRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var label = await FindLabelAsync(db, access, labelId, principal.GetUserId(), ct);
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(db, label.BoardId, name, label.Id, ct);

        label.Name = name;
        label.Color = request.Color;
        await db.SaveChangesAsync(ct);
        return new LabelDto(label.Id, label.Name, label.Color);
    }

    private static async Task<IResult> DeleteAsync(
        Guid labelId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var label = await FindLabelAsync(db, access, labelId, principal.GetUserId(), ct);
        db.Labels.Remove(label);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    internal static async Task<List<LabelDto>> ForBoardAsync(TaskboardDbContext db, Guid boardId, CancellationToken ct) =>
        await db.Labels
            .Where(l => l.BoardId == boardId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelDto(l.Id, l.Name, l.Color))
            .ToListAsync(ct);

    private static async Task EnsureUniqueNameAsync(TaskboardDbContext db, Guid boardId, string name, Guid? exceptId, CancellationToken ct)
    {
        // Boards only have a handful of labels, so compare in memory.
        var names = await db.Labels
            .Where(l => l.BoardId == boardId && l.Id != exceptId)
            .Select(l => l.Name)
            .ToListAsync(ct);
        if (names.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            throw new ConflictException($"This board already has a label called \"{name}\".");
        }
    }

    private static async Task<Label> FindLabelAsync(TaskboardDbContext db, WorkspaceAccess access, Guid labelId, Guid userId, CancellationToken ct)
    {
        var label = await db.Labels.SingleOrDefaultAsync(l => l.Id == labelId, ct)
            ?? throw new NotFoundException("Label not found.");
        try
        {
            await access.RequireBoardAsync(label.BoardId, userId, ct: ct);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException("Label not found.");
        }

        return label;
    }
}
