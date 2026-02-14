namespace Taskboard.Api.Domain;

public class BoardColumn
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public required string Name { get; set; }

    /// <summary>Zero-based, contiguous within a board.</summary>
    public int Position { get; set; }

    /// <summary>Soft work-in-progress limit. The board warns when it's exceeded; nothing is blocked.</summary>
    public int? WipLimit { get; set; }
}
