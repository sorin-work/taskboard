using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Comments;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class CommentTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private sealed record Setup(HttpClient Owner, HttpClient Member, Guid BoardId, Guid CardId);

    private async Task<Setup> SetupAsync(WorkspaceRole memberRole = WorkspaceRole.Member)
    {
        var (owner, _) = await factory.CreateUserAsync("Olive");
        var (member, memberUser) = await factory.CreateUserAsync("Max");
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, memberUser.Id, memberRole);
        var board = await owner.CreateBoardAsync(workspace.Id);
        var card = await owner.CreateCardAsync(board.Columns[0].Id);
        return new Setup(owner, member, board.Id, card.Id);
    }

    private static async Task<CommentDto> CommentAsync(HttpClient client, Guid cardId, string body)
    {
        var response = await client.PostAsJsonAsync($"/api/cards/{cardId}/comments", new CommentRequest(body));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<CommentDto>();
    }

    [Fact]
    public async Task Comments_AreListedOldestFirst()
    {
        var s = await SetupAsync();
        await CommentAsync(s.Owner, s.CardId, "First!");
        await CommentAsync(s.Member, s.CardId, "Second");

        var comments = await s.Owner.GetFromJsonAsync<List<CommentDto>>($"/api/cards/{s.CardId}/comments", TestJson.Options);

        comments!.Select(c => (c.Author.DisplayName, c.Body)).Should().Equal(("Olive", "First!"), ("Max", "Second"));
        (await s.Owner.GetCardsAsync(s.BoardId)).Single().CommentCount.Should().Be(2);
    }

    [Fact]
    public async Task Author_CanEdit()
    {
        var s = await SetupAsync();
        var comment = await CommentAsync(s.Member, s.CardId, "Typo hre");

        var response = await s.Member.PatchAsJsonAsync($"/api/comments/{comment.Id}", new CommentRequest("Typo here"));

        var edited = await response.ReadAsAsync<CommentDto>();
        edited.Body.Should().Be("Typo here");
        edited.EditedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task OthersCannotEdit_EvenOwners()
    {
        var s = await SetupAsync();
        var comment = await CommentAsync(s.Member, s.CardId, "Mine");

        var response = await s.Owner.PatchAsJsonAsync($"/api/comments/{comment.Id}", new CommentRequest("Changed"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_CanDeleteSomeoneElsesComment()
    {
        var s = await SetupAsync();
        var comment = await CommentAsync(s.Member, s.CardId, "Spam");

        var response = await s.Owner.DeleteAsync($"/api/comments/{comment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Member_CannotDeleteSomeoneElsesComment()
    {
        var s = await SetupAsync();
        var comment = await CommentAsync(s.Owner, s.CardId, "Keep me");

        var response = await s.Member.DeleteAsync($"/api/comments/{comment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithEmptyBody_ReturnsBadRequest()
    {
        var s = await SetupAsync();

        var response = await s.Owner.PostAsJsonAsync($"/api/cards/{s.CardId}/comments", new CommentRequest(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
