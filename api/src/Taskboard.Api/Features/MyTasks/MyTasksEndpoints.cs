using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Labels;

namespace Taskboard.Api.Features.MyTasks;

public sealed record MyTaskDto(
    Guid Id,
    string Title,
    DateOnly? DueDate,
    bool Completed,
    Guid BoardId,
    string BoardName,
    Guid WorkspaceId,
    string WorkspaceName,
    string ColumnName,
    IReadOnlyList<LabelDto> Labels);

public static class MyTasksEndpoints
{
    public static IEndpointRouteBuilder MapMyTasksEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me/tasks", ListAsync).WithTags("My tasks").RequireAuthorization();
        return app;
    }

    /// <summary>
    /// Cards assigned to the current user on every active board they can see.
    /// Soonest due first; cards without a due date go last.
    /// </summary>
    private static async Task<List<MyTaskDto>> ListAsync(
        bool? includeCompleted,
        string? due,
        DateOnly? today,
        ClaimsPrincipal principal,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var day = Today.Resolve(today, clock);

        var cards = db.Cards.Where(c =>
            c.Board.ArchivedAt == null &&
            db.WorkspaceMembers.Any(m => m.WorkspaceId == c.Board.WorkspaceId && m.UserId == userId));
        cards = new CardFilter("me", null, due, null).Apply(cards, userId, day);
        if (includeCompleted != true)
        {
            cards = cards.Where(c => c.CompletedAt == null);
        }

        return await cards
            .OrderBy(c => c.DueDate == null)
            .ThenBy(c => c.DueDate)
            .ThenBy(c => c.Board.Name)
            .ThenBy(c => c.Column.Position)
            .ThenBy(c => c.Position)
            .Select(c => new MyTaskDto(
                c.Id,
                c.Title,
                c.DueDate,
                c.CompletedAt != null,
                c.BoardId,
                c.Board.Name,
                c.Board.WorkspaceId,
                c.Board.Workspace.Name,
                c.Column.Name,
                c.Labels
                    .OrderBy(l => l.Label.Name)
                    .Select(l => new LabelDto(l.LabelId, l.Label.Name, l.Label.Color))
                    .ToList()))
            .ToListAsync(ct);
    }
}
