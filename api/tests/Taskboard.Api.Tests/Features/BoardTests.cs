using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Checklists;
using Taskboard.Api.Features.Labels;
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

    [Fact]
    public async Task Copy_WithoutCards_CopiesColumnsAndLabels()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var source = await client.CreateBoardAsync(workspace.Id, "Sprint 1", "Todo", "Doing", "Done");
        await client.PostAsJsonAsync($"/api/boards/{source.Id}/labels", new LabelRequest("Bug", "red"));
        await client.CreateCardAsync(source.Columns[0].Id, "Left behind");

        var response = await client.PostAsJsonAsync($"/api/boards/{source.Id}/copy", new CopyBoardRequest("Sprint 2", false));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var copy = await response.ReadAsAsync<BoardDto>();
        copy.Name.Should().Be("Sprint 2");
        copy.Columns.Select(c => c.Name).Should().Equal("Todo", "Doing", "Done");
        copy.Columns.Select(c => c.Id).Should().NotIntersectWith(source.Columns.Select(c => c.Id));
        copy.Labels.Select(l => l.Name).Should().Equal("Bug");
        (await client.GetCardsAsync(copy.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Copy_WithCards_KeepsOrderLabelsAndChecklists()
    {
        var (client, user) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var source = await client.CreateBoardAsync(workspace.Id, "Template", "Todo", "Done");
        var bug = await (await client.PostAsJsonAsync($"/api/boards/{source.Id}/labels", new LabelRequest("Bug", "red"))).ReadAsAsync<LabelDto>();
        var first = await client.CreateCardAsync(source.Columns[0].Id, "First");
        await client.CreateCardAsync(source.Columns[0].Id, "Second");
        await client.PutAsJsonAsync($"/api/cards/{first.Id}/labels", new SetCardLabelsRequest([bug.Id]));
        await client.PutAsJsonAsync($"/api/cards/{first.Id}/assignees", new SetCardAssigneesRequest([user.Id]));
        var checklist = await (await client.PostAsJsonAsync($"/api/cards/{first.Id}/checklists", new ChecklistRequest("Steps"))).ReadAsAsync<ChecklistDto>();
        await client.PostAsJsonAsync($"/api/checklists/{checklist.Id}/items", new CreateChecklistItemRequest("Step one"));

        var copy = await (await client.PostAsJsonAsync($"/api/boards/{source.Id}/copy", new CopyBoardRequest("From template", true))).ReadAsAsync<BoardDto>();

        var cards = await client.GetCardsAsync(copy.Id);
        cards.Select(c => c.Title).Should().Equal("First", "Second");
        cards.Should().OnlyContain(c => c.ColumnId == copy.Columns[0].Id);
        cards[0].LabelIds.Should().Equal(copy.Labels[0].Id);
        cards[0].AssigneeIds.Should().BeEmpty();
        cards[0].ChecklistTotal.Should().Be(1);
    }

    [Fact]
    public async Task Copy_ByNonMember_ReturnsNotFound()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (stranger, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var board = await owner.CreateBoardAsync(workspace.Id);

        var response = await stranger.PostAsJsonAsync($"/api/boards/{board.Id}/copy", new CopyBoardRequest("Mine now", true));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Star_PutsBoardFirstForThatUserOnly()
    {
        var (alice, _) = await factory.CreateUserAsync();
        var (bob, bobUser) = await factory.CreateUserAsync();
        var workspace = await alice.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, bobUser.Id);
        await alice.CreateBoardAsync(workspace.Id, "Alpha");
        var zulu = await alice.CreateBoardAsync(workspace.Id, "Zulu");

        (await alice.PutAsync($"/api/boards/{zulu.Id}/star", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await alice.PutAsync($"/api/boards/{zulu.Id}/star", null);

        var forAlice = await alice.GetFromJsonAsync<List<BoardSummaryDto>>($"/api/workspaces/{workspace.Id}/boards", TestJson.Options);
        var forBob = await bob.GetFromJsonAsync<List<BoardSummaryDto>>($"/api/workspaces/{workspace.Id}/boards", TestJson.Options);
        forAlice!.Select(b => (b.Name, b.Starred)).Should().Equal(("Zulu", true), ("Alpha", false));
        forBob!.Select(b => b.Name).Should().Equal("Alpha", "Zulu");
        (await alice.GetFromJsonAsync<BoardDto>($"/api/boards/{zulu.Id}", TestJson.Options))!.Starred.Should().BeTrue();
    }

    [Fact]
    public async Task Rename_KeepsReportingTheStar()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var board = await client.CreateBoardAsync(workspace.Id, "Old name");
        await client.PutAsync($"/api/boards/{board.Id}/star", null);

        var response = await client.PatchAsJsonAsync($"/api/boards/{board.Id}", new UpdateBoardRequest("New name", null, false));

        (await response.ReadAsAsync<BoardDto>()).Starred.Should().BeTrue();
    }

    [Fact]
    public async Task Unstar_RemovesTheStar()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var board = await client.CreateBoardAsync(workspace.Id);
        await client.PutAsync($"/api/boards/{board.Id}/star", null);

        await client.DeleteAsync($"/api/boards/{board.Id}/star");

        (await client.GetFromJsonAsync<BoardDto>($"/api/boards/{board.Id}", TestJson.Options))!.Starred.Should().BeFalse();
    }

    [Fact]
    public async Task List_CountsOverdueOpenCards()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var board = await client.CreateBoardAsync(workspace.Id);
        var today = new DateOnly(2026, 6, 19);
        async Task CardAsync(string title, DateOnly? due, bool completed = false)
        {
            var card = await client.CreateCardAsync(board.Columns[0].Id, title);
            await client.PutAsJsonAsync($"/api/cards/{card.Id}", new UpdateCardRequest(title, null, due, completed));
        }

        await CardAsync("late", today.AddDays(-1));
        await CardAsync("late but done", today.AddDays(-3), completed: true);
        await CardAsync("due today", today);
        await CardAsync("no date", null);

        var boards = await client.GetFromJsonAsync<List<BoardSummaryDto>>(
            $"/api/workspaces/{workspace.Id}/boards?today=2026-06-19", TestJson.Options);

        boards!.Single().CardCount.Should().Be(4);
        boards!.Single().OverdueCount.Should().Be(1);
    }
}
