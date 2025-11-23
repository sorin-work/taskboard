using System.Net.Http.Json;
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
}
