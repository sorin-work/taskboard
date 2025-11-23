using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Invitations;
using Taskboard.Api.Features.Workspaces;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class InvitationTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private static Task<HttpResponseMessage> InviteAsync(HttpClient client, Guid workspaceId, string email, WorkspaceRole role = WorkspaceRole.Member) =>
        client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/invitations", new CreateInvitationRequest(email, role), TestJson.Options);

    [Fact]
    public async Task Create_ReturnsTokenAndLink()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();

        var response = await InviteAsync(owner, workspace.Id, "New.Person@Example.com", WorkspaceRole.Admin);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await response.ReadAsAsync<CreatedInvitationDto>();
        invitation.Email.Should().Be("new.person@example.com");
        invitation.Role.Should().Be(WorkspaceRole.Admin);
        invitation.AcceptUrl.Should().EndWith($"/invite/{invitation.Token}");
    }

    [Fact]
    public async Task Create_ForOwnerRole_ReturnsBadRequest()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();

        var response = await InviteAsync(owner, workspace.Id, "x@example.com", WorkspaceRole.Owner);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ByMember_ReturnsForbidden()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (member, memberUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, memberUser.Id);

        var response = await InviteAsync(member, workspace.Id, "x@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ForExistingMember_ReturnsConflict()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();

        var response = await InviteAsync(owner, workspace.Id, ownerUser.Email);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Accept_AddsUserWithInvitedRole()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (invitee, inviteeUser) = await factory.CreateUserAsync("invitee");
        var workspace = await owner.CreateWorkspaceAsync("Studio");
        var invitation = await (await InviteAsync(owner, workspace.Id, inviteeUser.Email, WorkspaceRole.Admin)).ReadAsAsync<CreatedInvitationDto>();

        var response = await invitee.PostAsync($"/api/invitations/{invitation.Token}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var joined = await response.ReadAsAsync<WorkspaceDto>();
        joined.Name.Should().Be("Studio");
        joined.Role.Should().Be(WorkspaceRole.Admin);
        joined.MemberCount.Should().Be(2);
    }

    [Fact]
    public async Task Accept_Twice_ReturnsBadRequest()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (invitee, inviteeUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var invitation = await (await InviteAsync(owner, workspace.Id, inviteeUser.Email)).ReadAsAsync<CreatedInvitationDto>();
        await invitee.PostAsync($"/api/invitations/{invitation.Token}/accept", null);

        var response = await invitee.PostAsync($"/api/invitations/{invitation.Token}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_WithDifferentEmail_ReturnsForbidden()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (stranger, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var invitation = await (await InviteAsync(owner, workspace.Id, "someone-else@example.com")).ReadAsAsync<CreatedInvitationDto>();

        var response = await stranger.PostAsync($"/api/invitations/{invitation.Token}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accept_WhenExpired_ReturnsBadRequest()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (invitee, inviteeUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var invitation = await (await InviteAsync(owner, workspace.Id, inviteeUser.Email)).ReadAsAsync<CreatedInvitationDto>();
        factory.WithDb(db =>
        {
            db.Invitations.Single(i => i.Id == invitation.Id).ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            return db.SaveChanges();
        });

        var response = await invitee.PostAsync($"/api/invitations/{invitation.Token}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reinvite_RevokesPreviousLink()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (invitee, inviteeUser) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var first = await (await InviteAsync(owner, workspace.Id, inviteeUser.Email)).ReadAsAsync<CreatedInvitationDto>();
        await InviteAsync(owner, workspace.Id, inviteeUser.Email);

        var preview = await invitee.GetFromJsonAsync<InvitationPreviewDto>($"/api/invitations/{first.Token}", TestJson.Options);
        var pending = await owner.GetFromJsonAsync<List<InvitationDto>>($"/api/workspaces/{workspace.Id}/invitations", TestJson.Options);

        preview!.Status.Should().Be(InvitationStatus.Revoked);
        pending.Should().ContainSingle();
    }

    [Fact]
    public async Task Preview_WorksWithoutSigningIn()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync("Olga");
        var workspace = await owner.CreateWorkspaceAsync("Studio");
        var invitation = await (await InviteAsync(owner, workspace.Id, "guest@example.com")).ReadAsAsync<CreatedInvitationDto>();

        var preview = await factory.CreateClient().GetFromJsonAsync<InvitationPreviewDto>($"/api/invitations/{invitation.Token}", TestJson.Options);

        preview!.WorkspaceName.Should().Be("Studio");
        preview.InvitedBy.Should().Be(ownerUser.DisplayName);
        preview.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public async Task Preview_WithUnknownToken_ReturnsNotFound()
    {
        var response = await factory.CreateClient().GetAsync("/api/invitations/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Revoke_RemovesFromPendingList()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();
        var invitation = await (await InviteAsync(owner, workspace.Id, "gone@example.com")).ReadAsAsync<CreatedInvitationDto>();

        var response = await owner.DeleteAsync($"/api/workspaces/{workspace.Id}/invitations/{invitation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var pending = await owner.GetFromJsonAsync<List<InvitationDto>>($"/api/workspaces/{workspace.Id}/invitations", TestJson.Options);
        pending.Should().BeEmpty();
    }

    [Fact]
    public void Tokens_AreUniqueAndHashDeterministically()
    {
        var (a, hashA) = InvitationTokens.Generate();
        var (b, _) = InvitationTokens.Generate();

        a.Should().NotBe(b);
        a.Should().MatchRegex("^[A-Za-z0-9_-]{43}$");
        InvitationTokens.Hash(a).Should().Be(hashA).And.HaveLength(64);
    }
}
