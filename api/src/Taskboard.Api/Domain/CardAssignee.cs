namespace Taskboard.Api.Domain;

public class CardAssignee
{
    public Guid CardId { get; set; }

    public Card Card { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}
