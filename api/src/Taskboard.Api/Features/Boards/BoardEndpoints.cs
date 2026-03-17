using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Labels;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Boards;

public static class BoardEndpoints
{
    public static readonly IReadOnlyList<string> DefaultColumns = ["To do", "In progress", "Done"];

    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var workspace = app.MapGroup("/api/workspaces/{workspaceId:guid}/boards").WithTags("Boards").RequireAuthorization();
        workspace.MapGet("/", ListAsync);
        workspace.MapPost("/", CreateAsync).Validate<CreateBoardRequest>();

        var board = app.MapGroup("/api/boards/{boardId:guid}").WithTags("Boards").RequireAuthorization();
        board.MapGet("/", GetAsync);
        board.MapPatch("/", UpdateAsync).Validate<UpdateBoardRequest>();
        board.MapDelete("/", DeleteAsync);
        board.MapPost("/copy", CopyAsync).Validate<CopyBoardRequest>();

        return app;
    }

    private static async Task<List<BoardSummaryDto>> ListAsync(
        Guid workspaceId,
        bool? includeArchived,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireMemberAsync(workspaceId, principal.GetUserId(), ct: ct);
        var query = db.Boards.Where(b => b.WorkspaceId == workspaceId);
        if (includeArchived != true)
        {
            query = query.Where(b => b.ArchivedAt == null);
        }

        return await query
            .OrderBy(b => b.Name)
            .Select(b => new BoardSummaryDto(
                b.Id,
                b.WorkspaceId,
                b.Name,
                b.Description,
                db.Cards.Count(c => c.BoardId == b.Id),
                b.CreatedAt,
                b.ArchivedAt))
            .ToListAsync(ct);
    }

    private static async Task<IResult> CreateAsync(
        Guid workspaceId,
        CreateBoardRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var member = await access.RequireMemberAsync(workspaceId, userId, ct: ct);
        var columns = request.Columns is { Count: > 0 } ? request.Columns : DefaultColumns;

        var board = new Board
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            Columns = [.. columns.Select((name, i) => new BoardColumn { Id = Guid.NewGuid(), Name = name.Trim(), Position = i })],
        };
        db.Boards.Add(board);
        activity.Board(board.Id, userId, ActivityTypes.BoardCreated, new() { ["board"] = board.Name });
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/boards/{board.Id}", await LoadBoardAsync(db, board.Id, member.Role, ct));
    }

    private static async Task<BoardDto> GetAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var (board, member) = await access.RequireBoardAsync(boardId, principal.GetUserId(), ct: ct);
        return await LoadBoardAsync(db, board.Id, member.Role, ct);
    }

    private static async Task<BoardDto> UpdateAsync(
        Guid boardId,
        UpdateBoardRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var (board, member) = await access.RequireBoardAsync(boardId, principal.GetUserId(), ct: ct);
        if (request.Archived != (board.ArchivedAt is not null))
        {
            WorkspaceAccess.EnsureRole(member, WorkspaceRole.Admin);
            board.ArchivedAt = request.Archived ? clock.GetUtcNow().UtcDateTime : null;
        }

        board.Name = request.Name.Trim();
        board.Description = request.Description?.Trim() ?? string.Empty;
        await db.SaveChangesAsync(ct);

        return await LoadBoardAsync(db, board.Id, member.Role, ct);
    }

    private static async Task<IResult> DeleteAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var (board, _) = await access.RequireBoardAsync(boardId, principal.GetUserId(), WorkspaceRole.Admin, ct);
        db.Boards.Remove(board);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    /// <summary>
    /// Copies columns and labels, and optionally the cards with their labels
    /// and checklists. People, comments and history stay with the original.
    /// </summary>
    private static async Task<IResult> CopyAsync(
        Guid boardId,
        CopyBoardRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (source, member) = await access.RequireBoardAsync(boardId, userId, ct: ct);
        var now = clock.GetUtcNow().UtcDateTime;

        var columns = await db.Columns.AsNoTracking().Where(c => c.BoardId == source.Id).OrderBy(c => c.Position).ToListAsync(ct);
        var labels = await db.Labels.AsNoTracking().Where(l => l.BoardId == source.Id).ToListAsync(ct);

        var copy = new Board
        {
            Id = Guid.NewGuid(),
            WorkspaceId = source.WorkspaceId,
            Name = request.Name.Trim(),
            Description = source.Description,
            CreatedAt = now,
        };
        var columnMap = columns.ToDictionary(c => c.Id, c => new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = copy.Id,
            Name = c.Name,
            Position = c.Position,
            WipLimit = c.WipLimit,
        });
        var labelMap = labels.ToDictionary(l => l.Id, l => new Label { Id = Guid.NewGuid(), BoardId = copy.Id, Name = l.Name, Color = l.Color });
        copy.Columns = [.. columnMap.Values];
        db.Boards.Add(copy);
        db.Labels.AddRange(labelMap.Values);

        if (request.IncludeCards)
        {
            var cards = await db.Cards
                .AsNoTracking()
                .Include(c => c.Labels)
                .Include(c => c.Checklists).ThenInclude(l => l.Items)
                .Where(c => c.BoardId == source.Id)
                .AsSplitQuery()
                .ToListAsync(ct);
            foreach (var card in cards)
            {
                var cardId = Guid.NewGuid();
                db.Cards.Add(new Card
                {
                    Id = cardId,
                    BoardId = copy.Id,
                    ColumnId = columnMap[card.ColumnId].Id,
                    Title = card.Title,
                    Description = card.Description,
                    Position = card.Position,
                    DueDate = card.DueDate,
                    CompletedAt = card.CompletedAt,
                    CreatedById = userId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Labels = [.. card.Labels.Select(l => new CardLabel { CardId = cardId, LabelId = labelMap[l.LabelId].Id })],
                    Checklists = [.. card.Checklists.Select(l => new Checklist
                    {
                        Id = Guid.NewGuid(),
                        CardId = cardId,
                        Title = l.Title,
                        Position = l.Position,
                        Items = [.. l.Items.Select(i => new ChecklistItem { Id = Guid.NewGuid(), Text = i.Text, IsDone = i.IsDone, Position = i.Position })],
                    })],
                });
            }
        }

        activity.Board(copy.Id, userId, ActivityTypes.BoardCreated, new() { ["board"] = copy.Name, ["copiedFrom"] = source.Name });
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/boards/{copy.Id}", await LoadBoardAsync(db, copy.Id, member.Role, ct));
    }

    internal static async Task<BoardDto> LoadBoardAsync(TaskboardDbContext db, Guid boardId, WorkspaceRole role, CancellationToken ct)
    {
        var board = await db.Boards
            .AsNoTracking()
            .Include(b => b.Workspace)
            .Include(b => b.Columns)
            .SingleOrDefaultAsync(b => b.Id == boardId, ct)
            ?? throw new NotFoundException("Board not found.");

        var members = await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == board.WorkspaceId)
            .OrderBy(m => m.User.DisplayName)
            .Select(m => new BoardMemberDto(m.UserId, m.User.DisplayName, m.User.Email))
            .ToListAsync(ct);

        return new BoardDto(
            board.Id,
            board.WorkspaceId,
            board.Workspace.Name,
            board.Name,
            board.Description,
            role,
            board.CreatedAt,
            board.ArchivedAt,
            [.. board.Columns.OrderBy(c => c.Position).Select(ColumnDto.From)],
            await LabelEndpoints.ForBoardAsync(db, board.Id, ct),
            members);
    }
}
