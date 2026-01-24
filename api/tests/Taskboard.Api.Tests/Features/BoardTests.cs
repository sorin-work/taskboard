using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class BoardTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task Create_WithoutColumns_UsesDefaultColumns()
    {
        var (client, user) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync("Studio");

        var board = await client.CreateBoardAsync(workspace.Id, "Website");

        board.Name.Should().Be("Website");
        board.WorkspaceName.Should().Be("Studio");
        board.Columns.Select(c => c.Name).Should().Equal("To do", "In progress", "Done");
        board.Columns.Select(c => c.Position).Should().Equal(0, 1, 2);
        board.Members.Should().ContainSingle(m => m.UserId == user.Id);
    }

    [Fact]
    public async Task Create_WithColumns_KeepsTheirOrder()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();

        var board = await client.CreateBoardAsync(workspace.Id, "Hiring", "Applied", "Interview", "Offer");

        board.Columns.Select(c => c.Name).Should().Equal("Applied", "Interview", "Offer");
    }

    [Fact]
    public async Task List_HidesArchivedBoardsUnlessAsked()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var keep = await client.CreateBoardAsync(workspace.Id, "Keep");
        var old = await client.CreateBoardAsync(workspace.Id, "Old");
        await client.PatchAsJsonAsync($"/api/boards/{old.Id}", new UpdateBoardRequest("Old", null, Archived: true));

        var active = await client.GetFromJsonAsync<List<BoardSummaryDto>>($"/api/workspaces/{workspace.Id}/boards", TestJson.Options);
        var all = await client.GetFromJsonAsync<List<BoardSummaryDto>>($"/api/workspaces/{workspace.Id}/boards?includeArchived=true", TestJson.Options);

        active!.Select(b => b.Id).Should().Equal(keep.Id);
        all!.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_CountsCardsPerBoard()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var busy = await client.CreateBoardAsync(workspace.Id, "Busy");
        await client.CreateBoardAsync(workspace.Id, "Empty");
        await client.CreateCardAsync(busy.Columns[0].Id, "One");
        await client.CreateCardAsync(busy.Columns[2].Id, "Two");

        var boards = await client.GetFromJsonAsync<List<BoardSummaryDto>>($"/api/workspaces/{workspace.Id}/boards", TestJson.Options);

        boards!.Select(b => (b.Name, b.CardCount)).Should().Equal(("Busy", 2), ("Empty", 0));
    }

    [Fact]
    public async Task Get_ByNonMember_ReturnsNotFound()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (stranger, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var board = await owner.CreateBoardAsync(workspace.Id);

        var response = await stranger.GetAsync($"/api/boards/{board.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Member_CanRenameButNotArchive()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (member, memberUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, memberUser.Id);
        var board = await owner.CreateBoardAsync(workspace.Id);

        var rename = await member.PatchAsJsonAsync($"/api/boards/{board.Id}", new UpdateBoardRequest("Renamed", "Now with a description", false));
        var archive = await member.PatchAsJsonAsync($"/api/boards/{board.Id}", new UpdateBoardRequest("Renamed", null, true));

        rename.StatusCode.Should().Be(HttpStatusCode.OK);
        (await rename.ReadAsAsync<BoardDto>()).Description.Should().Be("Now with a description");
        archive.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ByMember_ReturnsForbidden()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (member, memberUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, memberUser.Id, WorkspaceRole.Member);
        var board = await owner.CreateBoardAsync(workspace.Id);

        var response = await member.DeleteAsync($"/api/boards/{board.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ByOwner_RemovesBoard()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var board = await owner.CreateBoardAsync(workspace.Id);

        var response = await owner.DeleteAsync($"/api/boards/{board.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await owner.GetAsync($"/api/boards/{board.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithBlankName_ReturnsBadRequest()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();

        var response = await client.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/boards", new CreateBoardRequest("", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
