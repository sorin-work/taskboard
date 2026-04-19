using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Cards;

/// <summary>Who's on a card and how it's labelled. Both are set as a whole list.</summary>
public static class CardPeopleEndpoints
{
    public static IEndpointRouteBuilder MapCardPeopleEndpoints(this IEndpointRouteBuilder app)
    {
        var card = app.MapGroup("/api/cards/{cardId:guid}").WithTags("Cards").RequireAuthorization();
        card.MapPut("/labels", SetLabelsAsync).Validate<SetCardLabelsRequest>();
        card.MapPut("/assignees", SetAssigneesAsync).Validate<SetCardAssigneesRequest>();
        return app;
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

        return await CardEndpoints.LoadCardAsync(db, cardId, ct);
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

        return await CardEndpoints.LoadCardAsync(db, cardId, ct);
    }
}
