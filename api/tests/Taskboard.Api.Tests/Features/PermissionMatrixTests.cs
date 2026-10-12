using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.Invitations;
using Taskboard.Api.Features.Workspaces;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

/// <summary>Who may do what, in one place. Each row runs as a fresh user with the given role.</summary>
public class PermissionMatrixTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    public enum Action
    {
        RenameWorkspace,
        DeleteWorkspace,
        Invite,
        ArchiveBoard,
        DeleteBoard,
        AddColumn,
        AddCard,
    }

    public static TheoryData<WorkspaceRole, Action, bool> Cases()
    {
        var data = new TheoryData<WorkspaceRole, Action, bool>();
        var allowed = new Dictionary<Action, WorkspaceRole>
        {
            [Action.RenameWorkspace] = WorkspaceRole.Admin,
            [Action.DeleteWorkspace] = WorkspaceRole.Owner,
            [Action.Invite] = WorkspaceRole.Admin,
            [Action.ArchiveBoard] = WorkspaceRole.Admin,
            [Action.DeleteBoard] = WorkspaceRole.Admin,
            [Action.AddColumn] = WorkspaceRole.Member,
            [Action.AddCard] = WorkspaceRole.Member,
        };
        foreach (var role in Enum.GetValues<WorkspaceRole>())
        {
            foreach (var (action, minimum) in allowed)
            {
                data.Add(role, action, role >= minimum);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Role_CanOnlyDoWhatItShould(WorkspaceRole role, Action action, bool expected)
    {
        var (owner, _) = await factory.CreateUserAsync("owner");
        var (actor, actorUser) = await factory.CreateUserAsync(role.ToString());
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, actorUser.Id, role);
        var board = await owner.CreateBoardAsync(workspace.Id);

        var response = action switch
        {
            Action.RenameWorkspace => await actor.PatchAsJsonAsync($"/api/workspaces/{workspace.Id}", new UpdateWorkspaceRequest("Renamed")),
            Action.DeleteWorkspace => await actor.DeleteAsync($"/api/workspaces/{workspace.Id}"),
            Action.Invite => await actor.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/invitations",
                new CreateInvitationRequest($"new-{Guid.NewGuid():N}@example.com", WorkspaceRole.Member), TestJson.Options),
            Action.ArchiveBoard => await actor.PatchAsJsonAsync($"/api/boards/{board.Id}", new UpdateBoardRequest(board.Name, null, true)),
            Action.DeleteBoard => await actor.DeleteAsync($"/api/boards/{board.Id}"),
            Action.AddColumn => await actor.PostAsJsonAsync($"/api/boards/{board.Id}/columns", new CreateColumnRequest("New")),
            Action.AddCard => await actor.PostAsJsonAsync($"/api/columns/{board.Columns[0].Id}/cards", new Taskboard.Api.Features.Cards.CreateCardRequest("New")),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        if (expected)
        {
            response.IsSuccessStatusCode.Should().BeTrue($"{role} should be able to {action}");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"{role} should not be able to {action}");
        }
    }
}
