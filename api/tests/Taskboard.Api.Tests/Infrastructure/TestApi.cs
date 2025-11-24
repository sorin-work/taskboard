using System.Net.Http.Json;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Workspaces;

namespace Taskboard.Api.Tests.Infrastructure;

/// <summary>Shortcuts for setting up data through the public API.</summary>
public static class TestApi
{
    public static async Task<WorkspaceDto> CreateWorkspaceAsync(this HttpClient client, string name = "Acme")
    {
        var response = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequest(name));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<WorkspaceDto>();
    }

    /// <summary>Adds a user straight to a workspace, skipping the invitation flow.</summary>
    public static void AddMember(this TaskboardApiFactory factory, Guid workspaceId, Guid userId, WorkspaceRole role = WorkspaceRole.Member)
    {
        factory.WithDb(db =>
        {
            db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
            });
            return db.SaveChanges();
        });
    }

    public static async Task<BoardDto> CreateBoardAsync(this HttpClient client, Guid workspaceId, string name = "Roadmap", params string[] columns)
    {
        var response = await client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/boards",
            new CreateBoardRequest(name, null, columns.Length > 0 ? columns : null));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<BoardDto>();
    }
}
