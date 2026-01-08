using System.Net.Http.Json;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.MyTasks;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class MyTasksTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private static readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task AssignAsync(HttpClient client, CardDto card, Guid userId, DateOnly? due = null, bool completed = false)
    {
        await client.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([userId]));
        if (due is not null || completed)
        {
            await client.PutAsJsonAsync($"/api/cards/{card.Id}", new UpdateCardRequest(card.Title, null, due, completed));
        }
    }

    private static async Task<List<MyTaskDto>> MyTasksAsync(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<List<MyTaskDto>>($"/api/me/tasks{query}", TestJson.Options))!;

    [Fact]
    public async Task ListsAssignedCardsAcrossWorkspaces_SoonestFirst()
    {
        var (me, user) = await factory.CreateUserAsync();
        var one = await me.CreateWorkspaceAsync("One");
        var two = await me.CreateWorkspaceAsync("Two");
        var boardA = await me.CreateBoardAsync(one.Id, "Alpha");
        var boardB = await me.CreateBoardAsync(two.Id, "Beta");
        await AssignAsync(me, await me.CreateCardAsync(boardA.Columns[0].Id, "No date"), user.Id);
        await AssignAsync(me, await me.CreateCardAsync(boardB.Columns[1].Id, "Next week"), user.Id, _today.AddDays(7));
        await AssignAsync(me, await me.CreateCardAsync(boardA.Columns[0].Id, "Tomorrow"), user.Id, _today.AddDays(1));
        await me.CreateCardAsync(boardA.Columns[0].Id, "Not mine");

        var tasks = await MyTasksAsync(me);

        tasks.Select(t => t.Title).Should().Equal("Tomorrow", "Next week", "No date");
        var nextWeek = tasks[1];
        nextWeek.BoardName.Should().Be("Beta");
        nextWeek.WorkspaceName.Should().Be("Two");
        nextWeek.ColumnName.Should().Be("In progress");
    }

    [Fact]
    public async Task HidesCompletedUnlessAsked()
    {
        var (me, user) = await factory.CreateUserAsync();
        var workspace = await me.CreateWorkspaceAsync();
        var board = await me.CreateBoardAsync(workspace.Id);
        await AssignAsync(me, await me.CreateCardAsync(board.Columns[0].Id, "Open"), user.Id);
        await AssignAsync(me, await me.CreateCardAsync(board.Columns[0].Id, "Done"), user.Id, completed: true);

        (await MyTasksAsync(me)).Select(t => t.Title).Should().Equal("Open");
        (await MyTasksAsync(me, "?includeCompleted=true")).Should().HaveCount(2);
    }

    [Fact]
    public async Task SkipsArchivedBoardsAndWorkspacesTheUserLeft()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (me, user) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, user.Id);
        var archived = await owner.CreateBoardAsync(workspace.Id, "Archived");
        await AssignAsync(owner, await owner.CreateCardAsync(archived.Columns[0].Id, "Old"), user.Id);
        await owner.PatchAsJsonAsync($"/api/boards/{archived.Id}", new UpdateBoardRequest("Archived", null, true));
        var active = await owner.CreateBoardAsync(workspace.Id, "Active");
        await AssignAsync(owner, await owner.CreateCardAsync(active.Columns[0].Id, "Current"), user.Id);

        (await MyTasksAsync(me)).Select(t => t.Title).Should().Equal("Current");

        await me.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{user.Id}");

        (await MyTasksAsync(me)).Should().BeEmpty();
    }

    [Fact]
    public async Task DueFilter_Applies()
    {
        var (me, user) = await factory.CreateUserAsync();
        var workspace = await me.CreateWorkspaceAsync();
        var board = await me.CreateBoardAsync(workspace.Id);
        await AssignAsync(me, await me.CreateCardAsync(board.Columns[0].Id, "Late"), user.Id, _today.AddDays(-1));
        await AssignAsync(me, await me.CreateCardAsync(board.Columns[0].Id, "Fine"), user.Id, _today.AddDays(3));

        (await MyTasksAsync(me, "?due=overdue")).Select(t => t.Title).Should().Equal("Late");
    }
}
