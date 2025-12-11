namespace Taskboard.Api.Domain;

public class Comment
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public Card Card { get; set; } = null!;

    public Guid AuthorId { get; set; }

    public User Author { get; set; } = null!;

    public required string Body { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? EditedAt { get; set; }
}
