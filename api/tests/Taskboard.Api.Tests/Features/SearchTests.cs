using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Search;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class SearchTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private static async Task<List<SearchResultDto>> SearchAsync(HttpClient client, Guid workspaceId, string q) =>
        (await client.GetFromJsonAsync<List<SearchResultDto>>($"/api/workspaces/{workspaceId}/search?q={Uri.EscapeDataString(q)}", TestJson.Options))!;

    [Fact]
    public async Task FindsCardsByTitleAndDescriptionAcrossBoards()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var web = await client.CreateBoardAsync(workspace.Id, "Website");
        var ops = await client.CreateBoardAsync(workspace.Id, "Ops");
        await client.CreateCardAsync(web.Columns[0].Id, "Invoice page layout");
        var described = await client.CreateCardAsync(ops.Columns[1].Id, "Monthly close");
        await client.PutAsJsonAsync($"/api/cards/{described.Id}",
            new UpdateCardRequest("Monthly close", "Send every INVOICE to accounting before the 5th."));
        await client.CreateCardAsync(ops.Columns[0].Id, "Unrelated");

        var results = await SearchAsync(client, workspace.Id, "invoice");

        results.Select(r => r.Title).Should().Equal("Invoice page layout", "Monthly close");
        results[0].BoardName.Should().Be("Website");
        results[0].Snippet.Should().BeNull();
        results[1].ColumnName.Should().Be("In progress");
        results[1].Snippet.Should().Contain("INVOICE to accounting");
    }

    [Fact]
    public async Task SkipsArchivedBoardsAndOtherWorkspaces()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();
        var other = await client.CreateWorkspaceAsync("Other");
        var archived = await client.CreateBoardAsync(workspace.Id, "Archived");
        await client.CreateCardAsync(archived.Columns[0].Id, "Roadmap old");
        await client.PatchAsJsonAsync($"/api/boards/{archived.Id}", new UpdateBoardRequest("Archived", null, true));
        var elsewhere = await client.CreateBoardAsync(other.Id);
        await client.CreateCardAsync(elsewhere.Columns[0].Id, "Roadmap elsewhere");

        (await SearchAsync(client, workspace.Id, "roadmap")).Should().BeEmpty();
    }

    [Fact]
    public async Task TooShortQuery_ReturnsBadRequest()
    {
        var (client, _) = await factory.CreateUserAsync();
        var workspace = await client.CreateWorkspaceAsync();

        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}/search?q=a");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NonMember_GetsNotFound()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (stranger, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();

        var response = await stranger.GetAsync($"/api/workspaces/{workspace.Id}/search?q=anything");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("short text with match", "match", "short text with match")]
    [InlineData("no hit here", "zzz", "no hit here")]
    public void Snippet_ReturnsTextAroundMatch(string text, string term, string expected) =>
        SearchEndpoints.Snippet(text, term).Should().Be(expected);

    [Fact]
    public void Snippet_TrimsLongTextAroundTheMatch()
    {
        var text = new string('a', 200) + " needle " + new string('b', 200);

        var snippet = SearchEndpoints.Snippet(text, "needle");

        snippet.Should().StartWith("...").And.EndWith("...").And.Contain("needle");
        snippet.Length.Should().BeLessThanOrEqualTo(126);
    }
}
