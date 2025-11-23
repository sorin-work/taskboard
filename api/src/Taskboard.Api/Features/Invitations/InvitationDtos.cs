using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Invitations;

public sealed record CreateInvitationRequest(string Email, WorkspaceRole Role);

public sealed record InvitationDto(Guid Id, string Email, WorkspaceRole Role, string InvitedBy, DateTime CreatedAt, DateTime ExpiresAt);

/// <summary>Returned once, when the invitation is created. The token can't be looked up again.</summary>
public sealed record CreatedInvitationDto(
    Guid Id,
    string Email,
    WorkspaceRole Role,
    string InvitedBy,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string Token,
    string AcceptUrl);

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Revoked,
}

public sealed record InvitationPreviewDto(
    Guid WorkspaceId,
    string WorkspaceName,
    string InvitedBy,
    string Email,
    WorkspaceRole Role,
    DateTime ExpiresAt,
    InvitationStatus Status);
