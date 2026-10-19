using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Taskboard.Api.Data;
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

    [Fact]
    public async Task Health_WhenDatabaseIsDown_Returns503()
    {
        var client = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<TaskboardDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TaskboardDbContext>>();
            services.AddDbContext<TaskboardDbContext>(o => o.UseSqlite("Data Source=/nonexistent/dir/taskboard.db;Mode=ReadOnly"));
        })).CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body.Should().Contain("database", "unreachable");
    }
}
