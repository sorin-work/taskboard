using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Workspaces;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class WorkspaceTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task Create_MakesCreatorOwner()
    {
        var (client, _) = await factory.CreateUserAsync();

        var workspace = await client.CreateWorkspaceAsync("  Design team ");

        workspace.Name.Should().Be("Design team");
        workspace.Role.Should().Be(WorkspaceRole.Owner);
        workspace.MemberCount.Should().Be(1);
    }

    [Fact]
    public async Task List_OnlyReturnsWorkspacesTheUserBelongsTo()
    {
        var (alice, _) = await factory.CreateUserAsync("alice");
        var (bob, _) = await factory.CreateUserAsync("bob");
        await alice.CreateWorkspaceAsync("Zeta");
        await alice.CreateWorkspaceAsync("Alpha");
        await bob.CreateWorkspaceAsync("Bob's");

        var list = await alice.GetFromJsonAsync<List<WorkspaceDto>>("/api/workspaces", TestJson.Options);

        list!.Select(w => w.Name).Should().Equal("Alpha", "Zeta");
    }

    [Fact]
    public async Task Get_ByNonMember_ReturnsNotFound()
    {
        var (alice, _) = await factory.CreateUserAsync("alice");
        var (bob, _) = await factory.CreateUserAsync("bob");
        var workspace = await alice.CreateWorkspaceAsync();

        var response = await bob.GetAsync($"/api/workspaces/{workspace.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rename_ByOwner_UpdatesName()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync("Old");

        var response = await client.PatchAsJsonAsync($"/api/workspaces/{workspace.Id}", new UpdateWorkspaceRequest("New"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadAsAsync<WorkspaceDto>()).Name.Should().Be("New");
    }

    [Fact]
    public async Task Rename_WithEmptyName_ReturnsBadRequest()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();

        var response = await client.PatchAsJsonAsync($"/api/workspaces/{workspace.Id}", new UpdateWorkspaceRequest(" "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ByOwner_RemovesWorkspace()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();

        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/workspaces/{workspace.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
