using System.Net;
using Microsoft.AspNetCore.Hosting;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests;

public class OpenApiTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task Document_IsServedInDevelopment()
    {
        var client = factory.WithWebHostBuilder(b => b.UseEnvironment("Development")).CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("/api/cards/{cardId}/move").And.Contain("/api/me/tasks");
    }

    [Fact]
    public async Task Document_IsHiddenOutsideDevelopment()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
