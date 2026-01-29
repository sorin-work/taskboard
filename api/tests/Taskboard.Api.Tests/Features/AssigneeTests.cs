using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class AssigneeTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task SetAssignees_AssignsWorkspaceMembers()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync("Owen");
        var (_, member) = await factory.CreateUserAsync("Mia");
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, member.Id);
        var board = await owner.CreateBoardAsync(workspace.Id);
        var card = await owner.CreateCardAsync(board.Columns[0].Id);

        var response = await owner.PutAsJsonAsync($"/api/cards/{card.Id}/assignees",
            new SetCardAssigneesRequest([ownerUser.Id, member.Id, member.Id]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadAsAsync<CardDto>();
        updated.Assignees.Select(a => a.DisplayName).Should().Equal("Mia", "Owen");
        (await owner.GetCardsAsync(board.Id)).Single().AssigneeIds.Should().BeEquivalentTo([ownerUser.Id, member.Id]);
    }

    [Fact]
    public async Task SetAssignees_WithOutsider_ReturnsBadRequest()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (_, outsider) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var board = await owner.CreateBoardAsync(workspace.Id);
        var card = await owner.CreateCardAsync(board.Columns[0].Id);

        var response = await owner.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([outsider.Id]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetAssignees_EmptyList_UnassignsEveryone()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var board = await owner.CreateBoardAsync(workspace.Id);
        var card = await owner.CreateCardAsync(board.Columns[0].Id);
        await owner.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([ownerUser.Id]));

        var response = await owner.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([]));

        (await response.ReadAsAsync<CardDto>()).Assignees.Should().BeEmpty();
    }

    [Fact]
    public async Task RemovingAMember_UnassignsThemOnlyInThatWorkspace()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync("Owen");
        var (member, memberUser) = await factory.CreateUserAsync("Mia");
        var workspace = await owner.CreateWorkspaceAsync("Shared");
        factory.AddMember(workspace.Id, memberUser.Id);
        var board = await owner.CreateBoardAsync(workspace.Id);
        var card = await owner.CreateCardAsync(board.Columns[0].Id);
        await owner.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([ownerUser.Id, memberUser.Id]));
        var own = await member.CreateWorkspaceAsync("Mia's");
        var ownBoard = await member.CreateBoardAsync(own.Id);
        var ownCard = await member.CreateCardAsync(ownBoard.Columns[0].Id);
        await member.PutAsJsonAsync($"/api/cards/{ownCard.Id}/assignees", new SetCardAssigneesRequest([memberUser.Id]));

        await owner.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{memberUser.Id}");

        var after = await owner.GetFromJsonAsync<CardDto>($"/api/cards/{card.Id}", TestJson.Options);
        after!.Assignees.Select(a => a.Id).Should().Equal(ownerUser.Id);
        var untouched = await member.GetFromJsonAsync<CardDto>($"/api/cards/{ownCard.Id}", TestJson.Options);
        untouched!.Assignees.Should().ContainSingle();
    }
}
