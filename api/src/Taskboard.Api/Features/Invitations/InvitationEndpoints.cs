using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Invitations;

public static class InvitationEndpoints
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var workspace = app.MapGroup("/api/workspaces/{workspaceId:guid}/invitations")
            .WithTags("Invitations")
            .RequireAuthorization();
        workspace.MapGet("/", ListAsync);
        workspace.MapPost("/", CreateAsync).Validate<CreateInvitationRequest>();
        workspace.MapDelete("/{invitationId:guid}", RevokeAsync);

        var byToken = app.MapGroup("/api/invitations/{token}").WithTags("Invitations");
        byToken.MapGet("/", PreviewAsync);
        byToken.MapPost("/accept", AcceptAsync).RequireAuthorization();

        return app;
    }

    private static async Task<List<InvitationDto>> ListAsync(
        Guid workspaceId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        await access.RequireMemberAsync(workspaceId, principal.GetUserId(), WorkspaceRole.Admin, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        return await db.Invitations
            .Where(i => i.WorkspaceId == workspaceId && i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now)
            .OrderBy(i => i.Email)
            .Select(i => new InvitationDto(i.Id, i.Email, i.Role, i.InvitedBy.DisplayName, i.CreatedAt, i.ExpiresAt))
            .ToListAsync(ct);
    }

    private static async Task<IResult> CreateAsync(
        Guid workspaceId,
        CreateInvitationRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        TimeProvider clock,
        IInvitationNotifier notifier,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        await access.RequireMemberAsync(workspaceId, userId, WorkspaceRole.Admin, ct);
        var email = EmailAddress.Normalize(request.Email);

        var alreadyMember = await db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.User.Email == email, ct);
        if (alreadyMember)
        {
            throw new ConflictException($"{email} is already a member of this workspace.");
        }

        var now = clock.GetUtcNow().UtcDateTime;

        // Inviting the same address again replaces the old link.
        var previous = await db.Invitations
            .Where(i => i.WorkspaceId == workspaceId && i.Email == email && i.AcceptedAt == null && i.RevokedAt == null)
            .ToListAsync(ct);
        previous.ForEach(i => i.RevokedAt = now);

        var (token, hash) = InvitationTokens.Generate();
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email = email,
            Role = request.Role,
            TokenHash = hash,
            InvitedById = userId,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
        };
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(ct);

        var inviter = await db.Users.Where(u => u.Id == userId).Select(u => u.DisplayName).SingleAsync(ct);
        var workspaceName = await db.Workspaces.Where(w => w.Id == workspaceId).Select(w => w.Name).SingleAsync(ct);
        var publicUrl = configuration["App:PublicUrl"]?.TrimEnd('/') ?? string.Empty;
        var acceptUrl = $"{publicUrl}/invite/{token}";
        await notifier.SendAsync(email, workspaceName, acceptUrl, ct);

        var dto = new CreatedInvitationDto(
            invitation.Id, email, invitation.Role, inviter, invitation.CreatedAt, invitation.ExpiresAt, token, acceptUrl);
        return Results.Created($"/api/workspaces/{workspaceId}/invitations/{invitation.Id}", dto);
    }

    private static async Task<IResult> RevokeAsync(
        Guid workspaceId,
        Guid invitationId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        await access.RequireMemberAsync(workspaceId, principal.GetUserId(), WorkspaceRole.Admin, ct);
        var invitation = await db.Invitations.SingleOrDefaultAsync(i => i.Id == invitationId && i.WorkspaceId == workspaceId, ct)
            ?? throw new NotFoundException("Invitation not found.");

        invitation.RevokedAt ??= clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<InvitationPreviewDto> PreviewAsync(string token, TaskboardDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var invitation = await FindByTokenAsync(db, token, ct);
        return new InvitationPreviewDto(
            invitation.WorkspaceId,
            invitation.Workspace.Name,
            invitation.InvitedBy.DisplayName,
            invitation.Email,
            invitation.Role,
            invitation.ExpiresAt,
            StatusOf(invitation, clock.GetUtcNow().UtcDateTime));
    }

    private static async Task<WorkspaceDto> AcceptAsync(
        string token,
        ClaimsPrincipal principal,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var invitation = await FindByTokenAsync(db, token, ct);
        var now = clock.GetUtcNow().UtcDateTime;

        switch (StatusOf(invitation, now))
        {
            case InvitationStatus.Expired:
                throw new BadRequestException("This invitation has expired. Ask for a new one.");
            case InvitationStatus.Revoked:
                throw new BadRequestException("This invitation was cancelled.");
            case InvitationStatus.Accepted:
                throw new BadRequestException("This invitation has already been used.");
        }

        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);
        if (!string.Equals(user.Email, invitation.Email, StringComparison.Ordinal))
        {
            throw new ForbiddenException($"This invitation was sent to {invitation.Email}. Sign in with that address to accept it.");
        }

        var existing = await db.WorkspaceMembers
            .SingleOrDefaultAsync(m => m.WorkspaceId == invitation.WorkspaceId && m.UserId == userId, ct);
        if (existing is null)
        {
            existing = new WorkspaceMember
            {
                WorkspaceId = invitation.WorkspaceId,
                UserId = userId,
                Role = invitation.Role,
                JoinedAt = now,
            };
            db.WorkspaceMembers.Add(existing);
        }

        invitation.AcceptedAt = now;
        await db.SaveChangesAsync(ct);

        var memberCount = await db.WorkspaceMembers.CountAsync(m => m.WorkspaceId == invitation.WorkspaceId, ct);
        return new WorkspaceDto(invitation.WorkspaceId, invitation.Workspace.Name, existing.Role, memberCount, invitation.Workspace.CreatedAt);
    }

    private static async Task<Invitation> FindByTokenAsync(TaskboardDbContext db, string token, CancellationToken ct)
    {
        var hash = InvitationTokens.Hash(token);
        return await db.Invitations
            .Include(i => i.Workspace)
            .Include(i => i.InvitedBy)
            .SingleOrDefaultAsync(i => i.TokenHash == hash, ct)
            ?? throw new NotFoundException("Invitation not found.");
    }

    private static InvitationStatus StatusOf(Invitation invitation, DateTime now) => invitation switch
    {
        { AcceptedAt: not null } => InvitationStatus.Accepted,
        { RevokedAt: not null } => InvitationStatus.Revoked,
        _ when invitation.ExpiresAt <= now => InvitationStatus.Expired,
        _ => InvitationStatus.Pending,
    };
}
