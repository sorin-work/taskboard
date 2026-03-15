using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Data;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Activity;

public sealed record ActivityDto(
    Guid Id,
    string Type,
    Guid? CardId,
    string? CardTitle,
    CardUserDto Actor,
    IReadOnlyDictionary<string, string> Data,
    DateTime CreatedAt);

public static class ActivityEndpoints
{
    public const int DefaultPageSize = 30;
    public const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/boards/{boardId:guid}/activity", BoardAsync).WithTags("Activity").RequireAuthorization();
        app.MapGet("/api/cards/{cardId:guid}/activity", CardAsync).WithTags("Activity").RequireAuthorization();
        return app;
    }

    /// <summary>
    /// Newest first. To get the next page pass the last entry you have as
    /// <paramref name="before"/> (its createdAt) and <paramref name="beforeId"/>.
    /// The id matters when several entries share a timestamp.
    /// </summary>
    private static async Task<List<ActivityDto>> BoardAsync(
        Guid boardId,
        DateTime? before,
        Guid? beforeId,
        int? limit,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireBoardAsync(boardId, principal.GetUserId(), ct: ct);
        var pageSize = Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize);
        var query = db.Activities.Where(a => a.BoardId == boardId);
        if (before is not { } cursor)
        {
            return await ProjectAsync(query, pageSize, ct);
        }

        var utc = cursor.ToUniversalTime();
        if (beforeId is not { } cursorId)
        {
            return await ProjectAsync(query.Where(a => a.CreatedAt < utc), pageSize, ct);
        }

        // Entries at exactly the cursor time come back in the same order as on
        // the previous page, so skip up to and including the cursor entry.
        var ties = await query.CountAsync(a => a.CreatedAt == utc, ct);
        var rows = await ProjectAsync(query.Where(a => a.CreatedAt <= utc), pageSize + ties, ct);
        var index = rows.FindIndex(a => a.Id == cursorId);
        return index < 0
            ? [.. rows.Where(a => a.CreatedAt < utc).Take(pageSize)]
            : [.. rows.Skip(index + 1).Take(pageSize)];
    }

    private static async Task<List<ActivityDto>> CardAsync(
        Guid cardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        return await ProjectAsync(db.Activities.Where(a => a.CardId == cardId), MaxPageSize, ct);
    }

    private static async Task<List<ActivityDto>> ProjectAsync(IQueryable<Domain.Activity> query, int limit, CancellationToken ct)
    {
        var rows = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(limit)
            .Select(a => new
            {
                a.Id,
                a.Type,
                a.CardId,
                CurrentTitle = a.Card != null ? a.Card.Title : null,
                a.ActorId,
                ActorName = a.Actor.DisplayName,
                a.Data,
                a.CreatedAt,
            })
            .ToListAsync(ct);

        return [.. rows.Select(r => new ActivityDto(
            r.Id,
            r.Type,
            r.CardId,
            r.CurrentTitle ?? r.Data.GetValueOrDefault("title"),
            new CardUserDto(r.ActorId, r.ActorName),
            r.Data,
            r.CreatedAt))];
    }
}
