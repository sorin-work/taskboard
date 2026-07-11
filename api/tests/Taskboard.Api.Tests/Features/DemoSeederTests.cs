using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Taskboard.Api.Features.Auth;
using Taskboard.Api.Features.Boards;
using Taskboard.Api.Features.MyTasks;
using Taskboard.Api.Features.Workspaces;
using Taskboard.Api.Seeding;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class DemoSeederTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private async Task SeedAsync()
    {
        factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync();
    }

    private async Task<HttpClient> LoginAsDemoAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(DemoSeeder.DemoEmail, DemoSeeder.DemoPassword));
        response.EnsureSuccessStatusCode();
        var auth = await response.ReadAsAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    [Fact]
    public async Task Seed_CreatesALoginWithWorkspacesBoardsAndTasks()
    {
        await SeedAsync();
        var client = await LoginAsDemoAsync();

        var workspaces = await client.GetFromJsonAsync<List<WorkspaceDto>>("/api/workspaces", TestJson.Options);
        workspaces!.Select(w => w.Name).Should().Equal("Home renovation", "Northwind Studio");

        var studio = workspaces!.Single(w => w.Name == "Northwind Studio");
        studio.MemberCount.Should().Be(4);
        var boards = await client.GetFromJsonAsync<List<BoardSummaryDto>>($"/api/workspaces/{studio.Id}/boards", TestJson.Options);
        boards!.Select(b => b.Name).Should().Contain("Website relaunch").And.NotContain("Old onboarding flow");

        var website = boards!.Single(b => b.Name == "Website relaunch");
        boards![0].Should().Be(website, "the demo user starred it");
        website.Starred.Should().BeTrue();
        (await client.GetCardsAsync(website.Id, "?archived=true")).Should().ContainSingle();
        var cards = await client.GetCardsAsync(website.Id);
        cards.Should().HaveCountGreaterThan(10);

        var tasks = await client.GetFromJsonAsync<List<MyTaskDto>>("/api/me/tasks", TestJson.Options);
        tasks.Should().Contain(t => t.DueDate < DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task Seed_RunsOnlyOnce()
    {
        await SeedAsync();
        var users = factory.WithDb(db => db.Users.Count());

        await SeedAsync();

        factory.WithDb(db => db.Users.Count()).Should().Be(users);
    }
}
