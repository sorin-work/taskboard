namespace Taskboard.Api.Domain;

public class Workspace
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<WorkspaceMember> Members { get; set; } = [];
}
