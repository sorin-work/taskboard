using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Features.Cards;
using Taskboard.Api.Features.Workspaces;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class WorkloadTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task Workload_CountsOpenOverdueAndThisWeekPerMember()
    {
        var (owner, ownerUser) = await factory.CreateUserAsync("Olga");
        var (_, idle) = await factory.CreateUserAsync("Ivan");
        var workspace = await owner.CreateWorkspaceAsync();
        factory.AddMember(workspace.Id, idle.Id);
        var board = await owner.CreateBoardAsync(workspace.Id);
        var today = new DateOnly(2026, 7, 9);

        async Task CardAsync(DateOnly? due, bool completed = false, bool archived = false)
        {
            var card = await owner.CreateCardAsync(board.Columns[0].Id);
            await owner.PutAsJsonAsync($"/api/cards/{card.Id}/assignees", new SetCardAssigneesRequest([ownerUser.Id]));
            await owner.PutAsJsonAsync($"/api/cards/{card.Id}", new UpdateCardRequest(card.Title, null, due, completed));
            if (archived)
            {
                await owner.PostAsync($"/api/cards/{card.Id}/archive", null);
            }
        }

        await CardAsync(today.AddDays(-2));
        await CardAsync(today.AddDays(3));
        await CardAsync(null);
        await CardAsync(today.AddDays(-5), completed: true);
        await CardAsync(today, archived: true);

        var workload = await owner.GetFromJsonAsync<List<WorkloadDto>>(
            $"/api/workspaces/{workspace.Id}/workload?today=2026-07-09", TestJson.Options);

        workload.Should().Equal(
            new WorkloadDto(ownerUser.Id, "Olga", 3, 1, 1),
            new WorkloadDto(idle.Id, "Ivan", 0, 0, 0));
    }

    [Fact]
    public async Task Workload_ForNonMember_IsNotFound()
    {
        var (owner, _) = await factory.CreateUserAsync();
        var (stranger, _) = await factory.CreateUserAsync();
        var workspace = await owner.CreateWorkspaceAsync();

        (await stranger.GetAsync($"/api/workspaces/{workspace.Id}/workload")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
