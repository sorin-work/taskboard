using System.Net.Http.Json;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.MyTasks;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class ArchiveTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private async Task<(HttpClient Client, BoardDto Board)> BoardAsync()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        return (client, await client.CreateBoardAsync(workspace.Id));
    }

    [Fact]
    public async Task Archive_HidesCardFromBoardButKeepsIt()
    {
        var (client, board) = await BoardAsync();
        await client.CreateCardAsync(board.Columns[0].Id, "Keep");
        var old = await client.CreateCardAsync(board.Columns[0].Id, "Old");

        var archived = await (await client.PostAsync($"/api/cards/{old.Id}/archive", null)).ReadAsAsync<CardDto>();

        archived.ArchivedAt.Should().NotBeNull();
        (await client.GetCardsAsync(board.Id)).Select(c => c.Title).Should().Equal("Keep");
        (await client.GetCardsAsync(board.Id, "?archived=true")).Select(c => c.Title).Should().Equal("Old");
        (await client.GetAsync($"/api/cards/{old.Id}")).IsSuccessStatusCode.Should().BeTrue();
        var summaries = await client.GetFromJsonAsync<List<BoardSummaryDto>>($"/api/workspaces/{board.WorkspaceId}/boards", TestJson.Options);
        summaries!.Single().CardCount.Should().Be(1);
    }

    [Fact]
    public async Task Restore_PutsCardAtTheBottomOfItsColumn()
    {
        var (client, board) = await BoardAsync();
        var first = await client.CreateCardAsync(board.Columns[0].Id, "First");
        await client.CreateCardAsync(board.Columns[0].Id, "Second");
        await client.PostAsync($"/api/cards/{first.Id}/archive", null);
        await client.CreateCardAsync(board.Columns[0].Id, "Third");

        var restored = await (await client.PostAsync($"/api/cards/{first.Id}/restore", null)).ReadAsAsync<CardDto>();

        restored.ArchivedAt.Should().BeNull();
        (await client.GetCardsAsync(board.Id)).Select(c => c.Title).Should().Equal("Second", "Third", "First");
    }

    [Fact]
    public async Task ArchiveAndRestore_AreLogged()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id);

        await client.PostAsync($"/api/cards/{card.Id}/archive", null);
        await client.PostAsync($"/api/cards/{card.Id}/archive", null);
        await client.PostAsync($"/api/cards/{card.Id}/restore", null);

        var feed = await (await client.GetAsync($"/api/cards/{card.Id}/activity")).ReadAsAsync<List<ActivityDto>>();
        feed.Select(a => a.Type).Should().Equal(ActivityTypes.CardRestored, ActivityTypes.CardArchived, ActivityTypes.CardCreated);
    }

    [Fact]
    public async Task ArchivedCards_LeaveMyTasks()
    {
        var (client, user) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var board = await client.CreateBoardAsync(workspace.Id);
        var card = await client.CreateCardAsync(board.Columns[0].Id, "Mine");
        await client.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([user.Id]));

        await client.PostAsync($"/api/cards/{card.Id}/archive", null);

        (await client.GetFromJsonAsync<List<MyTaskDto>>("/api/me/tasks", TestJson.Options)).Should().BeEmpty();
    }
}
