using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Checklists;

public static class ChecklistEndpoints
{
    public const int MaxItemsPerChecklist = 100;

    public static IEndpointRouteBuilder MapChecklistEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cards/{cardId:guid}/checklists", CreateAsync)
            .WithTags("Checklists")
            .RequireAuthorization()
            .Validate<ChecklistRequest>();

        var checklist = app.MapGroup("/api/checklists/{checklistId:guid}").WithTags("Checklists").RequireAuthorization();
        checklist.MapPatch("/", RenameAsync).Validate<ChecklistRequest>();
        checklist.MapDelete("/", DeleteAsync);
        checklist.MapPost("/items", AddItemAsync).Validate<CreateChecklistItemRequest>();

        var item = app.MapGroup("/api/checklist-items/{itemId:guid}").WithTags("Checklists").RequireAuthorization();
        item.MapPatch("/", UpdateItemAsync).Validate<UpdateChecklistItemRequest>();
        item.MapDelete("/", DeleteItemAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        Guid cardId,
        ChecklistRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var (card, _) = await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        var position = await db.Checklists.CountAsync(c => c.CardId == card.Id, ct);
        var checklist = new Checklist { Id = Guid.NewGuid(), CardId = card.Id, Title = request.Title.Trim(), Position = position };
        db.Checklists.Add(checklist);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/checklists/{checklist.Id}", ToDto(checklist));
    }

    private static async Task<ChecklistDto> RenameAsync(
        Guid checklistId,
        ChecklistRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var checklist = await FindChecklistAsync(db, access, checklistId, principal.GetUserId(), ct);
        checklist.Title = request.Title.Trim();
        await db.SaveChangesAsync(ct);
        return ToDto(checklist);
    }

    private static async Task<IResult> DeleteAsync(
        Guid checklistId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var checklist = await FindChecklistAsync(db, access, checklistId, principal.GetUserId(), ct);
        db.Checklists.Remove(checklist);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AddItemAsync(
        Guid checklistId,
        CreateChecklistItemRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var checklist = await FindChecklistAsync(db, access, checklistId, principal.GetUserId(), ct);
        if (checklist.Items.Count >= MaxItemsPerChecklist)
        {
            throw new ConflictException($"A checklist can have at most {MaxItemsPerChecklist} items.");
        }

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistId = checklist.Id,
            Text = request.Text.Trim(),
            Position = checklist.Items.Count == 0 ? 0 : checklist.Items.Max(i => i.Position) + 1,
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/checklist-items/{item.Id}", ToDto(item));
    }

    private static async Task<ChecklistItemDto> UpdateItemAsync(
        Guid itemId,
        UpdateChecklistItemRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var item = await FindItemAsync(db, access, itemId, userId, ct);
        if (request.Done && !item.IsDone)
        {
            var card = await db.Cards.SingleAsync(c => c.Id == item.Checklist.CardId, ct);
            activity.Card(card, userId, ActivityTypes.ChecklistItemCompleted, new() { ["item"] = request.Text.Trim() });
        }

        item.Text = request.Text.Trim();
        item.IsDone = request.Done;
        await db.SaveChangesAsync(ct);
        return ToDto(item);
    }

    private static async Task<IResult> DeleteItemAsync(
        Guid itemId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var item = await FindItemAsync(db, access, itemId, principal.GetUserId(), ct);
        db.ChecklistItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    internal static ChecklistDto ToDto(Checklist checklist) =>
        new(checklist.Id, checklist.Title, [.. checklist.Items.OrderBy(i => i.Position).Select(ToDto)]);

    private static ChecklistItemDto ToDto(ChecklistItem item) => new(item.Id, item.Text, item.IsDone, item.Position);

    private static async Task<Checklist> FindChecklistAsync(
        TaskboardDbContext db,
        WorkspaceAccess access,
        Guid checklistId,
        Guid userId,
        CancellationToken ct)
    {
        var checklist = await db.Checklists.Include(c => c.Items).SingleOrDefaultAsync(c => c.Id == checklistId, ct)
            ?? throw new NotFoundException("Checklist not found.");
        await RequireCardOrNotFoundAsync(access, checklist.CardId, userId, "Checklist not found.", ct);
        return checklist;
    }

    private static async Task<ChecklistItem> FindItemAsync(
        TaskboardDbContext db,
        WorkspaceAccess access,
        Guid itemId,
        Guid userId,
        CancellationToken ct)
    {
        var item = await db.ChecklistItems.Include(i => i.Checklist).SingleOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException("Checklist item not found.");
        await RequireCardOrNotFoundAsync(access, item.Checklist.CardId, userId, "Checklist item not found.", ct);
        return item;
    }

    private static async Task RequireCardOrNotFoundAsync(WorkspaceAccess access, Guid cardId, Guid userId, string message, CancellationToken ct)
    {
        try
        {
            await access.RequireCardAsync(cardId, userId, ct: ct);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException(message);
        }
    }
}
