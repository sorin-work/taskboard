using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Boards;

public static class ColumnEndpoints
{
    public const int MaxColumnsPerBoard = 20;

    public static IEndpointRouteBuilder MapColumnEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/boards/{boardId:guid}/columns", CreateAsync)
            .WithTags("Columns")
            .RequireAuthorization()
            .Validate<CreateColumnRequest>();

        var column = app.MapGroup("/api/columns/{columnId:guid}").WithTags("Columns").RequireAuthorization();
        column.MapPatch("/", RenameAsync).Validate<UpdateColumnRequest>();
        column.MapPost("/move", MoveAsync).Validate<MoveColumnRequest>();
        column.MapDelete("/", DeleteAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        Guid boardId,
        CreateColumnRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (board, _) = await access.RequireBoardAsync(boardId, userId, ct: ct);
        var count = await db.Columns.CountAsync(c => c.BoardId == board.Id, ct);
        if (count >= MaxColumnsPerBoard)
        {
            throw new ConflictException($"A board can have at most {MaxColumnsPerBoard} columns.");
        }

        var column = new BoardColumn { Id = Guid.NewGuid(), BoardId = board.Id, Name = request.Name.Trim(), Position = count };
        db.Columns.Add(column);
        activity.Board(board.Id, userId, ActivityTypes.ColumnCreated, new() { ["column"] = column.Name });
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/columns/{column.Id}", new ColumnDto(column.Id, column.Name, column.Position));
    }

    private static async Task<ColumnDto> RenameAsync(
        Guid columnId,
        UpdateColumnRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var column = (await access.RequireColumnAsync(columnId, userId, ct)).Column;
        var name = request.Name.Trim();
        if (name != column.Name)
        {
            activity.Board(column.BoardId, userId, ActivityTypes.ColumnRenamed, new() { ["from"] = column.Name, ["column"] = name });
        }

        column.Name = name;
        await db.SaveChangesAsync(ct);
        return new ColumnDto(column.Id, column.Name, column.Position);
    }

    private static async Task<List<ColumnDto>> MoveAsync(
        Guid columnId,
        MoveColumnRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var column = (await access.RequireColumnAsync(columnId, principal.GetUserId(), ct)).Column;
        var columns = await db.Columns.Where(c => c.BoardId == column.BoardId).OrderBy(c => c.Position).ToListAsync(ct);

        columns.Remove(column);
        columns.Insert(Math.Min(request.Position, columns.Count), column);
        for (var i = 0; i < columns.Count; i++)
        {
            columns[i].Position = i;
        }

        await db.SaveChangesAsync(ct);
        return [.. columns.Select(c => new ColumnDto(c.Id, c.Name, c.Position))];
    }

    private static async Task<IResult> DeleteAsync(
        Guid columnId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var column = (await access.RequireColumnAsync(columnId, userId, ct)).Column;
        activity.Board(column.BoardId, userId, ActivityTypes.ColumnDeleted, new() { ["column"] = column.Name });

        db.Columns.Remove(column);
        var rest = await db.Columns
            .Where(c => c.BoardId == column.BoardId && c.Id != column.Id)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);
        for (var i = 0; i < rest.Count; i++)
        {
            rest[i].Position = i;
        }

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
