using Taskboard.Api.Data;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Activity;

/// <summary>
/// Adds activity entries to the current unit of work. They are saved together
/// with the change they describe, so the log never mentions something that
/// didn't happen.
/// </summary>
public sealed class ActivityLog(TaskboardDbContext db, TimeProvider clock)
{
    public void Board(Guid boardId, Guid actorId, string type, Dictionary<string, string>? data = null) =>
        Add(boardId, null, actorId, type, data);

    public void Card(Card card, Guid actorId, string type, Dictionary<string, string>? data = null)
    {
        data ??= [];
        data.TryAdd("title", card.Title);
        Add(card.BoardId, card.Id, actorId, type, data);
    }

    private void Add(Guid boardId, Guid? cardId, Guid actorId, string type, Dictionary<string, string>? data)
    {
        db.Activities.Add(new Domain.Activity
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            CardId = cardId,
            ActorId = actorId,
            Type = type,
            Data = data ?? [],
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });
    }
}
