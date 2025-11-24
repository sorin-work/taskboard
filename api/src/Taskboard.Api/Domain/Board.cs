namespace Taskboard.Api.Domain;

public class Board
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;

    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public List<BoardColumn> Columns { get; set; } = [];
}
