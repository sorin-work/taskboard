using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Checklists;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class ChecklistTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private async Task<(HttpClient Client, Guid BoardId, CardDto Card)> CardAsync()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var board = await client.CreateBoardAsync(workspace.Id);
        return (client, board.Id, await client.CreateCardAsync(board.Columns[0].Id, "Launch"));
    }

    private static async Task<ChecklistDto> CreateChecklistAsync(HttpClient client, Guid cardId, string title = "Steps")
    {
        var response = await client.PostAsJsonAsync($"/api/cards/{cardId}/checklists", new ChecklistRequest(title));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<ChecklistDto>();
    }

    private static async Task<ChecklistItemDto> AddItemAsync(HttpClient client, Guid checklistId, string text)
    {
        var response = await client.PostAsJsonAsync($"/api/checklists/{checklistId}/items", new CreateChecklistItemRequest(text));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<ChecklistItemDto>();
    }

    [Fact]
    public async Task ItemsAppearInOrderOnTheCard()
    {
        var (client, _, card) = await CardAsync();
        var checklist = await CreateChecklistAsync(client, card.Id);
        await AddItemAsync(client, checklist.Id, "Write copy");
        await AddItemAsync(client, checklist.Id, "Get review");

        var after = await client.GetFromJsonAsync<CardDto>($"/api/cards/{card.Id}", TestJson.Options);

        after!.Checklists.Should().ContainSingle();
        after.Checklists[0].Items.Select(i => i.Text).Should().Equal("Write copy", "Get review");
    }

    [Fact]
    public async Task TickingItems_UpdatesCardProgress()
    {
        var (client, boardId, card) = await CardAsync();
        var checklist = await CreateChecklistAsync(client, card.Id);
        var first = await AddItemAsync(client, checklist.Id, "One");
        await AddItemAsync(client, checklist.Id, "Two");
        var other = await CreateChecklistAsync(client, card.Id, "QA");
        await AddItemAsync(client, other.Id, "Three");

        var response = await client.PatchAsJsonAsync($"/api/checklist-items/{first.Id}", new UpdateChecklistItemRequest("One", true));

        (await response.ReadAsAsync<ChecklistItemDto>()).Done.Should().BeTrue();
        var summary = (await client.GetCardsAsync(boardId)).Single();
        summary.ChecklistDone.Should().Be(1);
        summary.ChecklistTotal.Should().Be(3);
    }

    [Fact]
    public async Task RenameAndDeleteChecklist()
    {
        var (client, _, card) = await CardAsync();
        var checklist = await CreateChecklistAsync(client, card.Id);

        var renamed = await client.PatchAsJsonAsync($"/api/checklists/{checklist.Id}", new ChecklistRequest("Release steps"));
        var deleted = await client.DeleteAsync($"/api/checklists/{checklist.Id}");

        (await renamed.ReadAsAsync<ChecklistDto>()).Title.Should().Be("Release steps");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await client.GetFromJsonAsync<CardDto>($"/api/cards/{card.Id}", TestJson.Options);
        after!.Checklists.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteItem_RemovesIt()
    {
        var (client, _, card) = await CardAsync();
        var checklist = await CreateChecklistAsync(client, card.Id);
        var item = await AddItemAsync(client, checklist.Id, "Temp");

        var response = await client.DeleteAsync($"/api/checklist-items/{item.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await client.GetFromJsonAsync<CardDto>($"/api/cards/{card.Id}", TestJson.Options);
        after!.Checklists[0].Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Outsider_CannotTouchItems()
    {
        var (client, _, card) = await CardAsync();
        var checklist = await CreateChecklistAsync(client, card.Id);
        var item = await AddItemAsync(client, checklist.Id, "Private");
        var (stranger, _) = await factory.CreateUserAsync();

        var response = await stranger.PatchAsJsonAsync($"/api/checklist-items/{item.Id}", new UpdateChecklistItemRequest("Mine", true));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddItem_WithBlankText_ReturnsBadRequest()
    {
        var (client, _, card) = await CardAsync();
        var checklist = await CreateChecklistAsync(client, card.Id);

        var response = await client.PostAsJsonAsync($"/api/checklists/{checklist.Id}/items", new CreateChecklistItemRequest("  "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(2, 0, new[] { "c", "a", "b" })]
    [InlineData(0, 2, new[] { "b", "c", "a" })]
    [InlineData(0, 50, new[] { "b", "c", "a" })]
    public async Task MoveItem_Reorders(int from, int to, string[] expected)
    {
        var (client, _, card) = await CardAsync();
        var checklist = await CreateChecklistAsync(client, card.Id);
        var items = new List<ChecklistItemDto>();
        foreach (var text in new[] { "a", "b", "c" })
        {
            items.Add(await AddItemAsync(client, checklist.Id, text));
        }

        var response = await client.PostAsJsonAsync($"/api/checklist-items/{items[from].Id}/move", new MoveChecklistItemRequest(to));

        var moved = await response.ReadAsAsync<ChecklistDto>();
        moved.Items.Select(i => i.Text).Should().Equal(expected);
        moved.Items.Select(i => i.Position).Should().Equal(0, 1, 2);
    }
}
