using System.Net;
using System.Net.Http.Json;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests;

public class HealthTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body.Should().ContainKey("status").WhoseValue.Should().Be("ok");
        body.Should().ContainKey("database").WhoseValue.Should().Be("ok");
    }
}
