using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Workspaces;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class MemberTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task List_ReturnsOwnersFirst()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync("zoe");
        var (_, member) = await factory.CreateUserAsync("adam");
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, member.Id);

        var members = await owner.GetFromJsonAsync<List<MemberDto>>($"/api/workspaces/{workspace.Id}/members", TestJson.Options);

        members!.Select(m => m.UserId).Should().Equal(ownerUser.Id, member.Id);
        members![0].Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public async Task Admin_CanPromoteMemberToAdmin()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (admin, adminUser) = await factory.CreateUserAsync();
        var (_, member) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, adminUser.Id, WorkspaceRole.Admin);
        factory.AddMember(workspace.Id, member.Id);

        var response = await admin.PatchAsJsonAsync($"/api/workspaces/{workspace.Id}/members/{member.Id}",
            new UpdateMemberRequest(WorkspaceRole.Admin), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadAsAsync<MemberDto>()).Role.Should().Be(WorkspaceRole.Admin);
    }

    [Fact]
    public async Task Admin_CannotMakeSomeoneOwner()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (admin, adminUser) = await factory.CreateUserAsync();
        var (_, member) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, adminUser.Id, WorkspaceRole.Admin);
        factory.AddMember(workspace.Id, member.Id);

        var response = await admin.PatchAsJsonAsync($"/api/workspaces/{workspace.Id}/members/{member.Id}",
            new UpdateMemberRequest(WorkspaceRole.Owner), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Member_CannotChangeRoles()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (member, memberUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, memberUser.Id);

        var response = await member.PatchAsJsonAsync($"/api/workspaces/{workspace.Id}/members/{memberUser.Id}",
            new UpdateMemberRequest(WorkspaceRole.Admin), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LastOwner_CannotStepDown()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();

        var response = await owner.PatchAsJsonAsync($"/api/workspaces/{workspace.Id}/members/{ownerUser.Id}",
            new UpdateMemberRequest(WorkspaceRole.Admin), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Owner_CanStepDownWhenAnotherOwnerExists()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync();
        var (_, second) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, second.Id, WorkspaceRole.Owner);

        var response = await owner.PatchAsJsonAsync($"/api/workspaces/{workspace.Id}/members/{ownerUser.Id}",
            new UpdateMemberRequest(WorkspaceRole.Member), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Member_CanLeave()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (member, memberUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, memberUser.Id);

        var response = await member.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{memberUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await member.GetAsync($"/api/workspaces/{workspace.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Member_CannotRemoveOthers()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (member, memberUser) = await factory.CreateUserAsync();
        var (_, other) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, memberUser.Id);
        factory.AddMember(workspace.Id, other.Id);

        var response = await member.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{other.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CannotRemoveOwner()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync();
        var (admin, adminUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, adminUser.Id, WorkspaceRole.Admin);

        var response = await admin.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{ownerUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LastOwner_CannotLeave()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();

        var response = await owner.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{ownerUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
