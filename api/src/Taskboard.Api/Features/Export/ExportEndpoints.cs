using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Data;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Features.Export;

public static class ExportEndpoints
{
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/boards/{boardId:guid}/export.csv", ExportBoardAsync).WithTags("Export").RequireAuthorization();
        return app;
    }

    /// <summary>Every card on the board (archived ones too), one row each, in board order.</summary>
    private static async Task<IResult> ExportBoardAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        WorkspaceAccess access,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var (board, _) = await access.RequireBoardAsync(boardId, principal.GetUserId(), ct: ct);
        var cards = await db.Cards
            .AsNoTracking()
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.Column.Position)
            .ThenBy(c => c.Position)
            .Select(c => new
            {
                Column = c.Column.Name,
                c.Title,
                c.Description,
                Assignees = c.Assignees.Select(a => a.User.DisplayName).ToList(),
                Labels = c.Labels.Select(l => l.Label.Name).ToList(),
                c.DueDate,
                c.CompletedAt,
                c.ArchivedAt,
                Done = c.Checklists.SelectMany(l => l.Items).Count(i => i.IsDone),
                Total = c.Checklists.SelectMany(l => l.Items).Count(),
                c.CreatedAt,
            })
            .ToListAsync(ct);

        var csv = new CsvWriter().Row("Column", "Title", "Description", "Assignees", "Labels", "Due date", "Completed", "Archived", "Checklist", "Created");
        foreach (var c in cards)
        {
            csv.Row(
                c.Column,
                c.Title,
                c.Description,
                string.Join("; ", c.Assignees.Order(StringComparer.CurrentCulture)),
                string.Join("; ", c.Labels.Order(StringComparer.CurrentCulture)),
                c.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                c.CompletedAt is null ? "no" : "yes",
                c.ArchivedAt is null ? "no" : "yes",
                c.Total == 0 ? string.Empty : $"{c.Done}/{c.Total}",
                c.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        var fileName = $"{Slug(board.Name)}-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        // BOM so Excel opens it as UTF-8.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return Results.File(bytes, "text/csv; charset=utf-8", fileName);
    }

    internal static string Slug(string name)
    {
        var slug = new string([.. name.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')]);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return slug.Length == 0 ? "board" : slug;
    }
}
