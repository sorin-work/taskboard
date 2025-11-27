using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
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
            .Select(c => new CardSummaryDto(c.Id, c.ColumnId, c.Title, c.Position))
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
        card.UpdatedAt = clock.GetUtcNow().UtcDateTime;
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

    internal static async Task<CardDto> LoadCardAsync(TaskboardDbContext db, Guid cardId, CancellationToken ct)
    {
        return await db.Cards
            .Where(c => c.Id == cardId)
            .Select(c => new CardDto(
                c.Id,
                c.BoardId,
                c.ColumnId,
                c.Column.Name,
                c.Title,
                c.Description,
                c.Position,
                new CardUserDto(c.CreatedById, c.CreatedBy.DisplayName),
                c.CreatedAt,
                c.UpdatedAt))
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Card not found.");
    }
}
