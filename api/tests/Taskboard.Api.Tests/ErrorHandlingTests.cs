using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests;

public class ErrorHandlingTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    [Fact]
    public async Task MalformedJson_ReturnsBadRequest()
    {
        var (client, _) = await factory.CreateUserAsync();

        var response = await client.PostAsync("/api/workspaces", new StringContent("{\"name\": ", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadAsAsync<ProblemDetails>();
        problem.Detail.Should().Contain("JSON");
    }

    [Fact]
    public async Task WrongTypeInJson_ReturnsBadRequest()
    {
        var (client, _) = await factory.CreateUserAsync();

        var response = await client.PostAsync("/api/workspaces", new StringContent("{\"name\": 42}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetails()
    {
        var response = await factory.CreateClient().GetAsync("/api/nothing-here");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
