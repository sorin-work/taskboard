using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
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
        card.MapPost("/move", MoveAsync).Validate<MoveCardRequest>();
        card.MapPut("/labels", SetLabelsAsync).Validate<SetCardLabelsRequest>();
        card.MapPut("/assignees", SetAssigneesAsync).Validate<SetCardAssigneesRequest>();

        return app;
    }

    private static async Task<List<CardSummaryDto>> ListAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireBoardAsync(boardId, principal.GetUserId(), ct: ct);
        return await db.Cards
            .Where(c => c.BoardId == boardId)
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
                c.Checklists.SelectMany(l => l.Items).Count()))
            .ToListAsync(ct);
    }

    private static async Task<IResult> CreateAsync(
        Guid columnId,
        CreateCardRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var column = await db.Columns.SingleOrDefaultAsync(c => c.Id == columnId, ct)
            ?? throw new NotFoundException("Column not found.");
        await access.RequireBoardAsync(column.BoardId, userId, ct: ct);

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
            Position = CardPositions.Between(last, null),
            CreatedById = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Cards.Add(card);
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
        TimeProvider clock,
        CancellationToken ct)
    {
        var (card, _) = await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        card.Title = request.Title.Trim();
        card.Description = request.Description?.Trim() ?? string.Empty;
        card.DueDate = request.DueDate;
        var now = clock.GetUtcNow().UtcDateTime;
        if (request.Completed != (card.CompletedAt is not null))
        {
            card.CompletedAt = request.Completed ? now : null;
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
        CancellationToken ct)
    {
        var (card, _) = await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        db.Cards.Remove(card);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<CardDto> MoveAsync(
        Guid cardId,
        MoveCardRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var (card, _) = await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        var column = await db.Columns.SingleOrDefaultAsync(c => c.Id == request.ColumnId && c.BoardId == card.BoardId, ct)
            ?? throw new BadRequestException("The target column is not on this card's board.");

        var siblings = await db.Cards
            .Where(c => c.ColumnId == column.Id && c.Id != card.Id)
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

        card.ColumnId = column.Id;
        card.Position = CardPositions.Between(before, after);
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
        TimeProvider clock,
        CancellationToken ct)
    {
        var (card, _) = await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        var wanted = request.LabelIds.Distinct().ToList();
        var valid = await db.Labels.Where(l => l.BoardId == card.BoardId && wanted.Contains(l.Id)).Select(l => l.Id).ToListAsync(ct);
        if (valid.Count != wanted.Count)
        {
            throw new BadRequestException("Some of those labels don't belong to this board.");
        }

        var current = await db.CardLabels.Where(cl => cl.CardId == cardId).ToListAsync(ct);
        db.CardLabels.RemoveRange(current.Where(cl => !wanted.Contains(cl.LabelId)));
        db.CardLabels.AddRange(wanted
            .Where(id => current.All(cl => cl.LabelId != id))
            .Select(id => new CardLabel { CardId = cardId, LabelId = id }));
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
        TimeProvider clock,
        CancellationToken ct)
    {
        var (card, member) = await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        var wanted = request.UserIds.Distinct().ToList();
        var valid = await db.WorkspaceMembers
            .Where(m => m.WorkspaceId == member.WorkspaceId && wanted.Contains(m.UserId))
            .Select(m => m.UserId)
            .ToListAsync(ct);
        if (valid.Count != wanted.Count)
        {
            throw new BadRequestException("Cards can only be assigned to members of this workspace.");
        }

        var current = await db.CardAssignees.Where(a => a.CardId == cardId).ToListAsync(ct);
        db.CardAssignees.RemoveRange(current.Where(a => !wanted.Contains(a.UserId)));
        db.CardAssignees.AddRange(wanted
            .Where(id => current.All(a => a.UserId != id))
            .Select(id => new CardAssignee { CardId = cardId, UserId = id }));
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
