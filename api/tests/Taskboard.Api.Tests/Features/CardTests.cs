using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class CardTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private async Task<(HttpClient Client, BoardDto Board)> BoardAsync()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        return (client, await client.CreateBoardAsync(workspace.Id, "Board", "Todo", "Doing", "Done"));
    }

    [Fact]
    public async Task Create_AppendsToBottomOfColumn()
    {
        var (client, board) = await BoardAsync();
        var todo = board.Columns[0].Id;

        var first = await client.CreateCardAsync(todo, "First");
        var second = await client.CreateCardAsync(todo, " Second ");

        second.Title.Should().Be("Second");
        second.Position.Should().BeGreaterThan(first.Position);
        second.ColumnName.Should().Be("Todo");
        (await client.GetCardsAsync(board.Id)).Select(c => c.Title).Should().Equal("First", "Second");
    }

    [Fact]
    public async Task Update_ChangesTitleAndDescription()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id);

        var response = await client.PutAsJsonAsync($"/api/cards/{card.Id}", new UpdateCardRequest("Renamed", "Some **markdown**"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadAsAsync<CardDto>();
        updated.Title.Should().Be("Renamed");
        updated.Description.Should().Be("Some **markdown**");
    }

    [Fact]
    public async Task Delete_RemovesCard()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id);

        var response = await client.DeleteAsync($"/api/cards/{card.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/cards/{card.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ByNonMember_ReturnsNotFound()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id);
        var (stranger, _) = await factory.CreateUserAsync();

        var response = await stranger.GetAsync($"/api/cards/{card.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Move_ToTopOfSameColumn()
    {
        var (client, board) = await BoardAsync();
        var todo = board.Columns[0].Id;
        await client.CreateCardAsync(todo, "A");
        await client.CreateCardAsync(todo, "B");
        var c = await client.CreateCardAsync(todo, "C");

        var response = await client.MoveCardAsync(c.Id, todo, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetCardsAsync(board.Id)).Select(x => x.Title).Should().Equal("C", "A", "B");
    }

    [Fact]
    public async Task Move_BetweenTwoCards()
    {
        var (client, board) = await BoardAsync();
        var todo = board.Columns[0].Id;
        var a = await client.CreateCardAsync(todo, "A");
        await client.CreateCardAsync(todo, "B");
        var c = await client.CreateCardAsync(todo, "C");

        await client.MoveCardAsync(c.Id, todo, a.Id);

        (await client.GetCardsAsync(board.Id)).Select(x => x.Title).Should().Equal("A", "C", "B");
    }

    [Fact]
    public async Task Move_ToAnotherColumn()
    {
        var (client, board) = await BoardAsync();
        var (todo, doing) = (board.Columns[0].Id, board.Columns[1].Id);
        var a = await client.CreateCardAsync(todo, "A");
        var x = await client.CreateCardAsync(doing, "X");
        await client.CreateCardAsync(doing, "Y");

        var response = await client.MoveCardAsync(a.Id, doing, x.Id);

        var moved = await response.ReadAsAsync<CardDto>();
        moved.ColumnId.Should().Be(doing);
        moved.ColumnName.Should().Be("Doing");
        var cards = await client.GetCardsAsync(board.Id);
        cards.Where(c => c.ColumnId == doing).Select(c => c.Title).Should().Equal("X", "A", "Y");
        cards.Should().NotContain(c => c.ColumnId == todo);
    }

    [Fact]
    public async Task Move_IntoEmptyColumn()
    {
        var (client, board) = await BoardAsync();
        var a = await client.CreateCardAsync(board.Columns[0].Id, "A");

        await client.MoveCardAsync(a.Id, board.Columns[2].Id, null);

        (await client.GetCardsAsync(board.Id)).Should().ContainSingle(c => c.ColumnId == board.Columns[2].Id);
    }

    [Fact]
    public async Task Move_AfterCardInOtherColumn_ReturnsBadRequest()
    {
        var (client, board) = await BoardAsync();
        var a = await client.CreateCardAsync(board.Columns[0].Id, "A");
        var b = await client.CreateCardAsync(board.Columns[1].Id, "B");

        var response = await client.MoveCardAsync(a.Id, board.Columns[0].Id, b.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Move_ToColumnOnAnotherBoard_ReturnsBadRequest()
    {
        var (client, board) = await BoardAsync();
        var other = await client.CreateBoardAsync(board.WorkspaceId, "Other");
        var a = await client.CreateCardAsync(board.Columns[0].Id, "A");

        var response = await client.MoveCardAsync(a.Id, other.Columns[0].Id, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        var (client, board) = await BoardAsync();

        var response = await client.PostAsJsonAsync($"/api/columns/{board.Columns[0].Id}/cards", new CreateCardRequest(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_SetsAndClearsDueDate()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id);

        var set = await (await client.PutAsJsonAsync($"/api/cards/{card.Id}",
            new UpdateCardRequest(card.Title, null, new DateOnly(2026, 3, 14)))).ReadAsAsync<CardDto>();
        var cleared = await (await client.PutAsJsonAsync($"/api/cards/{card.Id}",
            new UpdateCardRequest(card.Title, null))).ReadAsAsync<CardDto>();

        set.DueDate.Should().Be(new DateOnly(2026, 3, 14));
        cleared.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task Update_CompletingKeepsOriginalTimestamp()
    {
        var (client, board) = await BoardAsync();
        var card = await client.CreateCardAsync(board.Columns[0].Id);

        var done = await (await client.PutAsJsonAsync($"/api/cards/{card.Id}",
            new UpdateCardRequest(card.Title, null, Completed: true))).ReadAsAsync<CardDto>();
        var again = await (await client.PutAsJsonAsync($"/api/cards/{card.Id}",
            new UpdateCardRequest("Still done", null, Completed: true))).ReadAsAsync<CardDto>();
        var reopened = await (await client.PutAsJsonAsync($"/api/cards/{card.Id}",
            new UpdateCardRequest("Reopened", null, Completed: false))).ReadAsAsync<CardDto>();

        done.CompletedAt.Should().NotBeNull();
        again.CompletedAt.Should().Be(done.CompletedAt);
        reopened.CompletedAt.Should().BeNull();
        (await client.GetCardsAsync(board.Id)).Single().Completed.Should().BeFalse();
    }
}
