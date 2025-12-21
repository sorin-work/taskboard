using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Comments;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var card = app.MapGroup("/api/cards/{cardId:guid}/comments").WithTags("Comments").RequireAuthorization();
        card.MapGet("/", ListAsync);
        card.MapPost("/", CreateAsync).Validate<CommentRequest>();

        var comment = app.MapGroup("/api/comments/{commentId:guid}").WithTags("Comments").RequireAuthorization();
        comment.MapPatch("/", UpdateAsync).Validate<CommentRequest>();
        comment.MapDelete("/", DeleteAsync);

        return app;
    }

    private static async Task<List<CommentDto>> ListAsync(
        Guid cardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireCardAsync(cardId, principal.GetUserId(), ct: ct);
        return await db.Comments
            .Where(c => c.CardId == cardId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.CardId, new CardUserDto(c.AuthorId, c.Author.DisplayName), c.Body, c.CreatedAt, c.EditedAt))
            .ToListAsync(ct);
    }

    private static async Task<IResult> CreateAsync(
        Guid cardId,
        CommentRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        ActivityLog activity,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (card, _) = await access.RequireCardAsync(cardId, userId, ct: ct);
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            CardId = card.Id,
            AuthorId = userId,
            Body = request.Body.Trim(),
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        db.Comments.Add(comment);
        activity.Card(card, userId, ActivityTypes.CommentAdded, new() { ["excerpt"] = Excerpt(comment.Body) });
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/comments/{comment.Id}", await LoadAsync(db, comment.Id, ct));
    }

    private static async Task<CommentDto> UpdateAsync(
        Guid commentId,
        CommentRequest request,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (comment, _) = await FindAsync(db, access, commentId, userId, ct);
        if (comment.AuthorId != userId)
        {
            throw new ForbiddenException("You can only edit your own comments.");
        }

        comment.Body = request.Body.Trim();
        comment.EditedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return await LoadAsync(db, comment.Id, ct);
    }

    private static async Task<IResult> DeleteAsync(
        Guid commentId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var (comment, member) = await FindAsync(db, access, commentId, userId, ct);
        if (comment.AuthorId != userId && member.Role < WorkspaceRole.Admin)
        {
            throw new ForbiddenException("Only the author or a workspace admin can delete this comment.");
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    internal static string Excerpt(string body)
    {
        var flat = string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return flat.Length <= 120 ? flat : string.Concat(flat.AsSpan(0, 117), "...");
    }

    private static async Task<CommentDto> LoadAsync(TaskboardDbContext db, Guid commentId, CancellationToken ct) =>
        await db.Comments
            .Where(c => c.Id == commentId)
            .Select(c => new CommentDto(c.Id, c.CardId, new CardUserDto(c.AuthorId, c.Author.DisplayName), c.Body, c.CreatedAt, c.EditedAt))
            .SingleAsync(ct);

    private static async Task<(Comment Comment, WorkspaceMember Member)> FindAsync(
        TaskboardDbContext db,
        WorkspaceAccess access,
        Guid commentId,
        Guid userId,
        CancellationToken ct)
    {
        var comment = await db.Comments.SingleOrDefaultAsync(c => c.Id == commentId, ct)
            ?? throw new NotFoundException("Comment not found.");
        try
        {
            var (_, member) = await access.RequireCardAsync(comment.CardId, userId, ct: ct);
            return (comment, member);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException("Comment not found.");
        }
    }
}
