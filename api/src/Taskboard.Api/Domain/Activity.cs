namespace Taskboard.Api.Domain;

/// <summary>Something that happened on a board, optionally about one card.</summary>
public class Activity
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    /// <summary>Null for board-level events, and once the card has been deleted.</summary>
    public Guid? CardId { get; set; }

    public Card? Card { get; set; }

    public Guid ActorId { get; set; }

    public User Actor { get; set; } = null!;

    public required string Type { get; set; }

    /// <summary>Event details, e.g. the card title or the columns it moved between.</summary>
    public Dictionary<string, string> Data { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
