namespace Taskboard.Api.Domain;

/// <summary>A board someone has starred, so it's listed first for them.</summary>
public class BoardStar
{
    public Guid UserId { get; set; }

    public Guid BoardId { get; set; }

    public Board Board { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
