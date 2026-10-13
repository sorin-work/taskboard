using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Search;

public sealed record SearchResultDto(
    Guid CardId,
    string Title,
    string? Snippet,
    Guid BoardId,
    string BoardName,
    string ColumnName,
    DateOnly? DueDate,
    bool Completed);

public static class SearchEndpoints
{
    public const int MaxResults = 50;

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/{workspaceId:guid}/search", SearchAsync).WithTags("Search").RequireAuthorization();
        return app;
    }

    /// <summary>
    /// Cards on any active board in the workspace whose title or description
    /// contains <paramref name="q"/>. Title matches come first.
    /// </summary>
    private static async Task<List<SearchResultDto>> SearchAsync(
        Guid workspaceId,
        string? q,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        await access.RequireMemberAsync(workspaceId, principal.GetUserId(), ct: ct);
        var term = q?.Trim().ToLowerInvariant() ?? string.Empty;
        if (term.Length < 2)
        {
            throw new BadRequestException("Search for at least two characters.");
        }

#pragma warning disable CA1304, CA1311, CA1862 // runs as SQL lower(), not in .NET
        var rows = await db.Cards
            .AsNoTracking()
            .Where(c => c.Board.WorkspaceId == workspaceId && c.Board.ArchivedAt == null && c.ArchivedAt == null)
            .Where(c => c.Title.ToLower().Contains(term) || c.Description.ToLower().Contains(term))
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                TitleMatch = c.Title.ToLower().Contains(term),
                c.BoardId,
                BoardName = c.Board.Name,
                ColumnName = c.Column.Name,
                c.DueDate,
                Completed = c.CompletedAt != null,
                c.UpdatedAt,
            })
            .OrderByDescending(c => c.TitleMatch)
            .ThenByDescending(c => c.UpdatedAt)
            .Take(MaxResults)
            .ToListAsync(ct);
#pragma warning restore CA1304, CA1311, CA1862

        return [.. rows.Select(r => new SearchResultDto(
            r.Id,
            r.Title,
            r.TitleMatch ? null : Snippet(r.Description, term),
            r.BoardId,
            r.BoardName,
            r.ColumnName,
            r.DueDate,
            r.Completed))];
    }

    /// <summary>About 120 characters of the description around the first match.</summary>
    internal static string Snippet(string text, string term)
    {
        var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return text.Length <= 120 ? text : string.Concat(text.AsSpan(0, 117), "...");
        }

        var start = Math.Max(0, index - 40);
        var length = Math.Min(text.Length - start, 120);
        var snippet = text.Substring(start, length).ReplaceLineEndings(" ");
        return (start > 0 ? "..." : string.Empty) + snippet + (start + length < text.Length ? "..." : string.Empty);
    }
}
