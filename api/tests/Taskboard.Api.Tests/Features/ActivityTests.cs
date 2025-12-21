using System.Net.Http.Json;
using Taskboard.Api.Features.Activity;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Comments;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class ActivityTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private async Task<(HttpClient Client, BoardDto Board)> BoardAsync()
    {
        var (client, _) = await factory.CreateUserAsync("Ana");
        var workspace = await client.CreateWorkspaceAsync();
        return (client, await client.CreateBoardAsync(workspace.Id, "Board", "Todo", "Done"));
    }

    private static async Task<List<ActivityDto>> BoardActivityAsync(HttpClient client, Guid boardId, string query = "") =>
        (await client.GetFromJsonAsync<List<ActivityDto>>($"/api/boards/{boardId}/activity{query}", TestJson.Options))!;

    [Fact]
    public async Task CreatingAndMovingCards_ShowsUpNewestFirst()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id, "Ship it");
        await client.MoveCardAsync(card.Id, board.Columns[1].Id, null);

        var feed = await BoardActivityAsync(client, board.Id);

        feed.Select(a => a.Type).Should().Equal(ActivityTypes.CardMoved, ActivityTypes.CardCreated, ActivityTypes.BoardCreated);
        var moved = feed[0];
        moved.Actor.DisplayName.Should().Be("Ana");
        moved.CardTitle.Should().Be("Ship it");
        moved.Data.Should().Contain("from", "Todo").And.Contain("to", "Done");
    }

    [Fact]
    public async Task ReorderingWithinColumn_IsNotLogged()
    {
        var (client, board) = await BoardAsync();
        await client.CreateCardAsync(board.Columns[0].Id, "A");
        var b = await client.CreateCardAsync(board.Columns[0].Id, "B");

        await client.MoveCardAsync(b.Id, board.Columns[0].Id, null);

        var feed = await (await client.GetAsync($"/api/cards/{b.Id}/activity")).ReadAsAsync<List<ActivityDto>>();
        feed.Select(x => x.Type).Should().Equal(ActivityTypes.CardCreated);
    }

    [Fact]
    public async Task EditingCard_LogsEachChange()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id, "Draft");

        await client.PutAsJsonAsync($"/api/cards/{card.Id}",
            new UpdateCardRequest("Final", "Details", new DateOnly(2026, 1, 31), Completed: true));

        var feed = await (await client.GetAsync($"/api/cards/{card.Id}/activity")).ReadAsAsync<List<ActivityDto>>();
        feed.Select(x => x.Type).Should().BeEquivalentTo(
        [
            ActivityTypes.CardCreated,
            ActivityTypes.CardRenamed,
            ActivityTypes.CardDescriptionChanged,
            ActivityTypes.CardDueDateSet,
            ActivityTypes.CardCompleted,
        ]);
        feed.Single(x => x.Type == ActivityTypes.CardRenamed).Data.Should().Contain("from", "Draft");
        feed.Single(x => x.Type == ActivityTypes.CardDueDateSet).Data.Should().Contain("dueDate", "2026-01-31");
    }

    [Fact]
    public async Task Comment_LogsAnExcerpt()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id);
        var longBody = string.Join(' ', Enumerable.Repeat("word", 60));

        await client.PostAsJsonAsync($"/api/cards/{card.Id}/comments", new CommentRequest(longBody));

        var entry = (await BoardActivityAsync(client, board.Id)).First();
        entry.Type.Should().Be(ActivityTypes.CommentAdded);
        entry.Data["excerpt"].Should().HaveLength(120).And.EndWith("...");
    }

    [Fact]
    public async Task DeletedCard_KeepsItsTitleInTheFeed()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id, "Short lived");

        await client.DeleteAsync($"/api/cards/{card.Id}");

        var feed = await BoardActivityAsync(client, board.Id);
        feed[0].Type.Should().Be(ActivityTypes.CardDeleted);
        feed[0].CardTitle.Should().Be("Short lived");
        feed.Single(a => a.Type == ActivityTypes.CardCreated).CardTitle.Should().Be("Short lived");
    }

    [Fact]
    public async Task Feed_PagesWithBefore()
    {
        var (client, board) = await BoardAsync();
        for (var i = 0; i < 5; i++)
        {
            await client.CreateCardAsync(board.Columns[0].Id, $"Card {i}");
        }

        var first = await BoardActivityAsync(client, board.Id, "?limit=3");
        var cursor = Uri.EscapeDataString(first[^1].CreatedAt.ToString("O"));
        var second = await BoardActivityAsync(client, board.Id, $"?limit=3&before={cursor}");

        first.Should().HaveCount(3);
        second.Select(a => a.Id).Should().NotIntersectWith(first.Select(a => a.Id));
        first.Concat(second).Should().HaveCount(6);
    }
}
