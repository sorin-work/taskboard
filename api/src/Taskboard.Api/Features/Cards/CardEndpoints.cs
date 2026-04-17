using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Checklists;
using Taskboard.Api.Features.Labels;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Cards;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/boards/{boardId:guid}/cards", ListAsync).WithTags("Cards").RequireAuthorization();
        app.MapPost("/api/columns/{columnId:guid}/cards", CreateAsync)
            .WithTags("Cards")
            .RequireAuthorization()
            .Validate<CreateCardRequest>();

        var card = app.MapGroup("/api/cards/{cardId:guid}").WithTags("Cards").RequireAuthorization();
        card.MapGet("/", GetAsync);
        card.MapPut("/", UpdateAsync).Validate<UpdateCardRequest>();
        card.MapDelete("/", DeleteAsync);
        card.MapPost("/archive", ArchiveAsync);
        card.MapPost("/restore", RestoreAsync);
        card.MapPost("/move", MoveAsync).Validate<MoveCardRequest>();
        card.MapPut("/labels", SetLabelsAsync).Validate<SetCardLabelsRequest>();
        card.MapPut("/assignees", SetAssigneesAsync).Validate<SetCardAssigneesRequest>();

        return app;
    }

    private static async Task<List<CardSummaryDto>> ListAsync(
        Guid boardId,
        [AsParameters] CardFilter filter,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        await access.RequireBoardAsync(boardId, userId, ct: ct);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        return await filter.Apply(db.Cards.Where(c => c.BoardId == boardId), userId, today)
            .OrderBy(c => c.Column.Position)
            .ThenBy(c => c.Position)
            .ThenBy(c => c.CreatedAt)
            .Select(c => new CardSummaryDto(
                c.Id,
                c.ColumnId,
                c.Title,
                c.Position,
                c.DueDate,
                c.CompletedAt != null,
                c.Assignees.Select(a => a.UserId).ToList(),
                c.Labels.Select(l => l.LabelId).ToList(),
                c.Checklists.SelectMany(l => l.Items).Count(i => i.IsDone),
                c.Checklists.SelectMany(l => l.Items).Count(),
                c.Comments.Count))
            .ToListAsync(ct);
    }

    private static async Task<IResult> CreateAsync(
        Guid columnId,
        CreateCardRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (column, _) = await access.RequireColumnAsync(columnId, userId, ct);

        var last = await db.Cards
            .Where(c => c.ColumnId == columnId)
            .MaxAsync(c => (long?)c.Position, ct);

        var now = clock.GetUtcNow().UtcDateTime;
        var card = new Card
        {
            Id = Guid.NewGuid(),
            BoardId = column.BoardId,
            ColumnId = column.Id,
            Title = request.Title.Trim(),
            Position = CardPositions.Between(last, null)!.Value,
            CreatedById = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Cards.Add(card);
        activity.Card(card, userId, ActivityTypes.CardCreated, new() { ["column"] = column.Name });
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/cards/{card.Id}", await LoadCardAsync(db, card.Id, ct));
    }

    private static async Task<CardDto> GetAsync(
        Guid cardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        return await LoadCardAsync(db, cardId, ct);
    }

    private static async Task<CardDto> UpdateAsync(
        Guid cardId,
        UpdateCardRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (card, _) = await access.RequireCardAsync(cardId, userId, ct: ct);
        var now = clock.GetUtcNow().UtcDateTime;

        var title = request.Title.Trim();
        if (title != card.Title)
        {
            activity.Card(card, userId, ActivityTypes.CardRenamed, new() { ["from"] = card.Title, ["title"] = title });
            card.Title = title;
        }

        var description = request.Description?.Trim() ?? string.Empty;
        if (description != card.Description)
        {
            activity.Card(card, userId, ActivityTypes.CardDescriptionChanged);
            card.Description = description;
        }

        if (request.DueDate != card.DueDate)
        {
            card.DueDate = request.DueDate;
            if (request.DueDate is { } due)
            {
                activity.Card(card, userId, ActivityTypes.CardDueDateSet, new() { ["dueDate"] = due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
            }
            else
            {
                activity.Card(card, userId, ActivityTypes.CardDueDateCleared);
            }
        }

        if (request.Completed != (card.CompletedAt is not null))
        {
            card.CompletedAt = request.Completed ? now : null;
            activity.Card(card, userId, request.Completed ? ActivityTypes.CardCompleted : ActivityTypes.CardReopened);
        }

        card.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return await LoadCardAsync(db, cardId, ct);
    }

    private static async Task<IResult> DeleteAsync(
        Guid cardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (card, _) = await access.RequireCardAsync(cardId, userId, ct: ct);
        activity.Board(card.BoardId, userId, ActivityTypes.CardDeleted, new() { ["title"] = card.Title });
        db.Cards.Remove(card);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<CardDto> ArchiveAsync(
        Guid cardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (card, _) = await access.RequireCardAsync(cardId, userId, ct: ct);
        if (card.ArchivedAt is null)
        {
            card.ArchivedAt = clock.GetUtcNow().UtcDateTime;
            activity.Card(card, userId, ActivityTypes.CardArchived);
            await db.SaveChangesAsync(ct);
        }

        return await LoadCardAsync(db, cardId, ct);
    }

    /// <summary>Puts an archived card back, at the bottom of the column it was in.</summary>
    private static async Task<CardDto> RestoreAsync(
        Guid cardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (card, _) = await access.RequireCardAsync(cardId, userId, ct: ct);
        if (card.ArchivedAt is not null)
        {
            var last = await db.Cards
                .Where(c => c.ColumnId == card.ColumnId && c.ArchivedAt == null)
                .MaxAsync(c => (long?)c.Position, ct);
            card.Position = CardPositions.Between(last, null)!.Value;
            card.ArchivedAt = null;
            activity.Card(card, userId, ActivityTypes.CardRestored);
            await db.SaveChangesAsync(ct);
        }

        return await LoadCardAsync(db, cardId, ct);
    }

    private static async Task<CardDto> MoveAsync(
        Guid cardId,
        MoveCardRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (card, _) = await access.RequireCardAsync(cardId, userId, ct: ct);
        var column = await db.Columns.SingleOrDefaultAsync(c => c.Id == request.ColumnId && c.BoardId == card.BoardId, ct)
            ?? throw new BadRequestException("The target column is not on this card's board.");

        var siblings = await db.Cards
            .Where(c => c.ColumnId == column.Id && c.Id != card.Id && c.ArchivedAt == null)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var index = 0;
        if (request.AfterCardId is { } afterId)
        {
            var afterIndex = siblings.FindIndex(c => c.Id == afterId);
            if (afterIndex < 0)
            {
                throw new BadRequestException("The card to place this one after is not in the target column.");
            }

            index = afterIndex + 1;
        }

        long? before = index > 0 ? siblings[index - 1].Position : null;
        long? after = index < siblings.Count ? siblings[index].Position : null;
        var position = CardPositions.Between(before, after);
        if (position is null)
        {
            CardPositions.Renumber(siblings, (c, p) => c.Position = p);
            position = CardPositions.Between(siblings[index - 1].Position, siblings[index].Position);
        }

        if (card.ColumnId != column.Id)
        {
            var from = await db.Columns.Where(c => c.Id == card.ColumnId).Select(c => c.Name).SingleAsync(ct);
            activity.Card(card, userId, ActivityTypes.CardMoved, new() { ["from"] = from, ["to"] = column.Name });
        }

        card.ColumnId = column.Id;
        card.Position = position!.Value;
        card.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return await LoadCardAsync(db, cardId, ct);
    }

    private static async Task<CardDto> SetLabelsAsync(
        Guid cardId,
        SetCardLabelsRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (card, _) = await access.RequireCardAsync(cardId, userId, ct: ct);
        var wanted = request.LabelIds.Distinct().ToList();
        var labels = await db.Labels.Where(l => l.BoardId == card.BoardId && wanted.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
        if (labels.Count != wanted.Count)
        {
            throw new BadRequestException("Some of those labels don't belong to this board.");
        }

        var current = await db.CardLabels.Include(cl => cl.Label).Where(cl => cl.CardId == cardId).ToListAsync(ct);
        foreach (var removed in current.Where(cl => !wanted.Contains(cl.LabelId)))
        {
            db.CardLabels.Remove(removed);
            activity.Card(card, userId, ActivityTypes.CardLabelRemoved, new() { ["label"] = removed.Label.Name });
        }

        foreach (var id in wanted.Where(id => current.All(cl => cl.LabelId != id)))
        {
            db.CardLabels.Add(new CardLabel { CardId = cardId, LabelId = id });
            activity.Card(card, userId, ActivityTypes.CardLabelAdded, new() { ["label"] = labels[id].Name });
        }

        card.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return await LoadCardAsync(db, cardId, ct);
    }

    private static async Task<CardDto> SetAssigneesAsync(
        Guid cardId,
        SetCardAssigneesRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (card, member) = await access.RequireCardAsync(cardId, userId, ct: ct);
        var wanted = request.UserIds.Distinct().ToList();
        var names = await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == member.WorkspaceId && wanted.Contains(m.UserId))
            .Select(m => new { m.UserId, m.User.DisplayName })
            .ToDictionaryAsync(m => m.UserId, m => m.DisplayName, ct);
        if (names.Count != wanted.Count)
        {
            throw new BadRequestException("Cards can only be assigned to members of this workspace.");
        }

        var current = await db.CardAssignees.Include(a => a.User).Where(a => a.CardId == cardId).ToListAsync(ct);
        foreach (var removed in current.Where(a => !wanted.Contains(a.UserId)))
        {
            db.CardAssignees.Remove(removed);
            activity.Card(card, userId, ActivityTypes.CardUnassigned, new() { ["user"] = removed.User.DisplayName });
        }

        foreach (var id in wanted.Where(id => current.All(a => a.UserId != id)))
        {
            db.CardAssignees.Add(new CardAssignee { CardId = cardId, UserId = id });
            activity.Card(card, userId, ActivityTypes.CardAssigned, new() { ["user"] = names[id] });
        }

        card.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return await LoadCardAsync(db, cardId, ct);
    }

    internal static async Task<CardDto> LoadCardAsync(TaskboardDbContext db, Guid cardId, CancellationToken ct)
    {
        var checklists = await db.Checklists
            .AsNoTracking()
            .Include(c => c.Items)
            .Where(c => c.CardId == cardId)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        var card = await db.Cards
            .Where(c => c.Id == cardId)
            .Select(c => new CardDto(
                c.Id,
                c.BoardId,
                c.ColumnId,
                c.Column.Name,
                c.Title,
                c.Description,
                c.Position,
                c.DueDate,
                c.CompletedAt,
                c.ArchivedAt,
                new CardUserDto(c.CreatedById, c.CreatedBy.DisplayName),
                c.Assignees
                    .OrderBy(a => a.User.DisplayName)
                    .Select(a => new CardUserDto(a.UserId, a.User.DisplayName))
                    .ToList(),
                c.Labels
                    .OrderBy(l => l.Label.Name)
                    .Select(l => new LabelDto(l.LabelId, l.Label.Name, l.Label.Color))
                    .ToList(),
                new List<ChecklistDto>(),
                c.CreatedAt,
                c.UpdatedAt))
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Card not found.");

        return card with { Checklists = [.. checklists.Select(ChecklistEndpoints.ToDto)] };
    }
}
