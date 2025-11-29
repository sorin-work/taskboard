namespace Taskboard.Api.Domain;

public class Label
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public required string Name { get; set; }

    /// <summary>One of <see cref="LabelColors.All"/>.</summary>
    public required string Color { get; set; }
}

public class CardLabel
{
    public Guid CardId { get; set; }

    public Card Card { get; set; } = null!;

    public Guid LabelId { get; set; }

    public Label Label { get; set; } = null!;
}

public static class LabelColors
{
    public static readonly IReadOnlyList<string> All = ["gray", "red", "orange", "yellow", "green", "teal", "blue", "purple", "pink"];
}
