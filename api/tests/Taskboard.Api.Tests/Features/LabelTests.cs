using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Labels;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class LabelTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private async Task<(HttpClient Client, BoardDto Board)> BoardAsync()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        return (client, await client.CreateBoardAsync(workspace.Id));
    }

    private static async Task<LabelDto> CreateLabelAsync(HttpClient client, Guid boardId, string name, string color = "blue")
    {
        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/labels", new LabelRequest(name, color));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<LabelDto>();
    }

    [Fact]
    public async Task Create_ShowsUpOnBoardSortedByName()
    {
        var (client, board) = await BoardAsync();
        await CreateLabelAsync(client, board.Id, "Frontend", "purple");
        await CreateLabelAsync(client, board.Id, "Bug", "red");

        var after = await client.GetFromJsonAsync<BoardDto>($"/api/boards/{board.Id}", TestJson.Options);

        after!.Labels.Select(l => (l.Name, l.Color)).Should().Equal(("Bug", "red"), ("Frontend", "purple"));
    }

    [Fact]
    public async Task Create_WithUnknownColor_ReturnsBadRequest()
    {
        var (client, board) = await BoardAsync();

        var response = await client.PostAsJsonAsync($"/api/boards/{board.Id}/labels", new LabelRequest("Bug", "#ff0000"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithDuplicateNameInAnyCase_ReturnsConflict()
    {
        var (client, board) = await BoardAsync();
        await CreateLabelAsync(client, board.Id, "Bug");

        var response = await client.PostAsJsonAsync($"/api/boards/{board.Id}/labels", new LabelRequest("BUG", "red"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ChangesNameAndColor()
    {
        var (client, board) = await BoardAsync();
        var label = await CreateLabelAsync(client, board.Id, "Bug");

        var response = await client.PatchAsJsonAsync($"/api/labels/{label.Id}", new LabelRequest("Defect", "orange"));

        (await response.ReadAsAsync<LabelDto>()).Should().Be(new LabelDto(label.Id, "Defect", "orange"));
    }

    [Fact]
    public async Task Delete_RemovesLabelFromCards()
    {
        var (client, board) = await BoardAsync();
        var label = await CreateLabelAsync(client, board.Id, "Bug");
        var card = await client.CreateCardAsync(board.Columns[0].Id);
        await client.PutAsJsonAsync($"/api/cards/{card.Id}/labels", new SetCardLabelsRequest([label.Id]));

        var response = await client.DeleteAsync($"/api/labels/{label.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await client.GetFromJsonAsync<CardDto>($"/api/cards/{card.Id}", TestJson.Options);
        after!.Labels.Should().BeEmpty();
    }

    [Fact]
    public async Task SetCardLabels_ReplacesTheList()
    {
        var (client, board) = await BoardAsync();
        var bug = await CreateLabelAsync(client, board.Id, "Bug", "red");
        var ui = await CreateLabelAsync(client, board.Id, "UI", "teal");
        var card = await client.CreateCardAsync(board.Columns[0].Id);

        await client.PutAsJsonAsync($"/api/cards/{card.Id}/labels", new SetCardLabelsRequest([bug.Id, ui.Id]));
        var response = await client.PutAsJsonAsync($"/api/cards/{card.Id}/labels", new SetCardLabelsRequest([ui.Id]));

        (await response.ReadAsAsync<CardDto>()).Labels.Should().Equal(ui);
        (await client.GetCardsAsync(board.Id)).Single().LabelIds.Should().Equal(ui.Id);
    }

    [Fact]
    public async Task SetCardLabels_FromAnotherBoard_ReturnsBadRequest()
    {
        var (client, board) = await BoardAsync();
        var other = await client.CreateBoardAsync(board.WorkspaceId, "Other");
        var foreign = await CreateLabelAsync(client, other.Id, "Elsewhere");
        var card = await client.CreateCardAsync(board.Columns[0].Id);

        var response = await client.PutAsJsonAsync($"/api/cards/{card.Id}/labels", new SetCardLabelsRequest([foreign.Id]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
