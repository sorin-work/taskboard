using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class ColumnTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private async Task<(HttpClient Client, BoardDto Board)> BoardAsync(params string[] columns)
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        return (client, await client.CreateBoardAsync(workspace.Id, "Board", columns));
    }

    private static async Task<List<string>> ColumnNamesAsync(HttpClient client, Guid boardId)
    {
        var board = await client.GetFromJsonAsync<BoardDto>($"/api/boards/{boardId}", TestJson.Options);
        return [.. board!.Columns.Select(c => c.Name)];
    }

    [Fact]
    public async Task Create_AppendsToTheEnd()
    {
        var (client, board) = await BoardAsync("A", "B");

        var response = await client.PostAsJsonAsync($"/api/boards/{board.Id}/columns", new CreateColumnRequest("C"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadAsAsync<ColumnDto>()).Position.Should().Be(2);
        (await ColumnNamesAsync(client, board.Id)).Should().Equal("A", "B", "C");
    }

    [Fact]
    public async Task Rename_ChangesName()
    {
        var (client, board) = await BoardAsync("A", "B");

        var response = await client.PatchAsJsonAsync($"/api/columns/{board.Columns[1].Id}", new UpdateColumnRequest("Blocked"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ColumnNamesAsync(client, board.Id)).Should().Equal("A", "Blocked");
    }

    [Theory]
    [InlineData(0, 0, new[] { "A", "B", "C", "D" })]
    [InlineData(0, 2, new[] { "B", "C", "A", "D" })]
    [InlineData(3, 0, new[] { "D", "A", "B", "C" })]
    [InlineData(1, 99, new[] { "A", "C", "D", "B" })]
    public async Task Move_ReordersColumns(int from, int to, string[] expected)
    {
        var (client, board) = await BoardAsync("A", "B", "C", "D");

        var response = await client.PostAsJsonAsync($"/api/columns/{board.Columns[from].Id}/move", new MoveColumnRequest(to));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ColumnNamesAsync(client, board.Id)).Should().Equal(expected);
    }

    [Fact]
    public async Task Delete_ClosesTheGap()
    {
        var (client, board) = await BoardAsync("A", "B", "C");

        var response = await client.DeleteAsync($"/api/columns/{board.Columns[1].Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await client.GetFromJsonAsync<BoardDto>($"/api/boards/{board.Id}", TestJson.Options);
        after!.Columns.Select(c => (c.Name, c.Position)).Should().Equal(("A", 0), ("C", 1));
    }

    [Fact]
    public async Task Rename_ByNonMember_ReturnsNotFound()
    {
        var (_, board) = await BoardAsync("A");
        var (stranger, _) = await factory.CreateUserAsync();

        var response = await stranger.PatchAsJsonAsync($"/api/columns/{board.Columns[0].Id}", new UpdateColumnRequest("Mine"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WipLimit_CanBeSetAndCleared()
    {
        var (client, board) = await BoardAsync("Doing");
        var column = board.Columns[0];

        var set = await client.PutAsJsonAsync($"/api/columns/{column.Id}/wip-limit", new SetWipLimitRequest(3));
        var afterSet = await client.GetFromJsonAsync<BoardDto>($"/api/boards/{board.Id}", TestJson.Options);
        var cleared = await client.PutAsJsonAsync($"/api/columns/{column.Id}/wip-limit", new SetWipLimitRequest(null));

        (await set.ReadAsAsync<ColumnDto>()).WipLimit.Should().Be(3);
        afterSet!.Columns[0].WipLimit.Should().Be(3);
        (await cleared.ReadAsAsync<ColumnDto>()).WipLimit.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task WipLimit_OutOfRange_ReturnsBadRequest(int limit)
    {
        var (client, board) = await BoardAsync("Doing");

        var response = await client.PutAsJsonAsync($"/api/columns/{board.Columns[0].Id}/wip-limit", new SetWipLimitRequest(limit));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WipLimit_DoesNotBlockAddingCards()
    {
        var (client, board) = await BoardAsync("Doing");
        var column = board.Columns[0];
        await client.PutAsJsonAsync($"/api/columns/{column.Id}/wip-limit", new SetWipLimitRequest(1));

        await client.CreateCardAsync(column.Id, "One");
        var second = await client.PostAsJsonAsync($"/api/columns/{column.Id}/cards", new Taskboard.Api.Features.Cards.CreateCardRequest("Two"));

        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
