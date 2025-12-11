namespace Taskboard.Api.Domain;

public class Checklist
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public Card Card { get; set; } = null!;

    public required string Title { get; set; }

    public int Position { get; set; }

    public List<ChecklistItem> Items { get; set; } = [];
}

public class ChecklistItem
{
    public Guid Id { get; set; }

    public Guid ChecklistId { get; set; }

    public Checklist Checklist { get; set; } = null!;

    public required string Text { get; set; }

    public bool IsDone { get; set; }

    public int Position { get; set; }
}
