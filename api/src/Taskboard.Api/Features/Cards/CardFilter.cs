using Taskboard.Api.Common;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Cards;

/// <summary>
/// Query string filters for card lists:
/// <c>assignee</c> = a user id, <c>me</c> or <c>none</c>;
/// <c>label</c> = a label id;
/// <c>due</c> = <c>overdue</c>, <c>today</c>, <c>week</c> or <c>none</c>;
/// <c>q</c> = text in the title;
/// <c>archived</c> = true to list archived cards instead of the board.
/// </summary>
public sealed record CardFilter(string? Assignee, Guid? Label, string? Due, string? Q, bool? Archived = null)
{
    public static readonly IReadOnlyList<string> DueOptions = ["overdue", "today", "week", "none"];

    public IQueryable<Card> Apply(IQueryable<Card> cards, Guid currentUserId, DateOnly today)
    {
        cards = Archived == true ? cards.Where(c => c.ArchivedAt != null) : cards.Where(c => c.ArchivedAt == null);

        switch (Assignee?.Trim().ToLowerInvariant())
        {
            case null or "":
                break;
            case "me":
                cards = cards.Where(c => c.Assignees.Any(a => a.UserId == currentUserId));
                break;
            case "none":
                cards = cards.Where(c => c.Assignees.Count == 0);
                break;
            case var raw when Guid.TryParse(raw, out var userId):
                cards = cards.Where(c => c.Assignees.Any(a => a.UserId == userId));
                break;
            default:
                throw new BadRequestException("assignee must be a user id, \"me\" or \"none\".");
        }

        if (Label is { } labelId)
        {
            cards = cards.Where(c => c.Labels.Any(l => l.LabelId == labelId));
        }

        var weekEnd = today.AddDays(6);
        cards = Due?.Trim().ToLowerInvariant() switch
        {
            null or "" => cards,
            "overdue" => cards.Where(c => c.DueDate < today && c.CompletedAt == null),
            "today" => cards.Where(c => c.DueDate == today),
            "week" => cards.Where(c => c.DueDate >= today && c.DueDate <= weekEnd),
            "none" => cards.Where(c => c.DueDate == null),
            _ => throw new BadRequestException($"due must be one of: {string.Join(", ", DueOptions)}."),
        };

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim().ToLowerInvariant();
#pragma warning disable CA1304, CA1311, CA1862 // runs as SQL lower(), not in .NET
            cards = cards.Where(c => c.Title.ToLower().Contains(term));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return cards;
    }
}
