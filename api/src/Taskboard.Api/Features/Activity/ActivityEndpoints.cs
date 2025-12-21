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

    /// <summary>Newest first. Pass the oldest createdAt you have as <paramref name="before"/> to get the next page.</summary>
    private static async Task<List<ActivityDto>> BoardAsync(
        Guid boardId,
        DateTime? before,
        int? limit,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireBoardAsync(boardId, principal.GetUserId(), ct: ct);
        var query = db.Activities.Where(a => a.BoardId == boardId);
        if (before is { } cursor)
        {
            var utc = cursor.ToUniversalTime();
            query = query.Where(a => a.CreatedAt < utc);
        }

        return await ProjectAsync(query, Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize), ct);
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
