namespace Taskboard.Api.Domain;

public class Invitation
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;

    public required string Email { get; set; }

    public WorkspaceRole Role { get; set; }

    /// <summary>SHA-256 of the token in the invite link. The raw token is never stored.</summary>
    public required string TokenHash { get; set; }

    public Guid InvitedById { get; set; }

    public User InvitedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsPending(DateTime now) => AcceptedAt is null && RevokedAt is null && ExpiresAt > now;
}
