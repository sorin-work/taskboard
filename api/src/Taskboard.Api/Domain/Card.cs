namespace Taskboard.Api.Domain;

public class Card
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public Guid ColumnId { get; set; }

    public BoardColumn Column { get; set; } = null!;

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Sort key within the column. Gaps are intentional, see CardPositions.</summary>
    public long Position { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid CreatedById { get; set; }

    public User CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
