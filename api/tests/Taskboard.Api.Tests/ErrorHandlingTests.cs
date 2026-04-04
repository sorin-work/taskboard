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

    [Fact]
    public async Task Responses_CarryARequestId_ThatMatchesTheProblemTraceId()
    {
        var response = await factory.CreateClient().GetAsync("/api/nothing-here");

        var id = response.Headers.GetValues("X-Request-Id").Single();
        id.Should().MatchRegex("^[0-9a-f]{32}$");
        var problem = await response.ReadAsAsync<ProblemDetails>();
        problem.Extensions["traceId"]!.ToString().Should().Be(id);
    }

    [Theory]
    [InlineData("from-nginx-123", "from-nginx-123")]
    [InlineData("has spaces and <html>", null)]
    public async Task IncomingRequestId_IsKeptOnlyWhenSafe(string incoming, string? expected)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Request-Id", incoming);

        var response = await client.GetAsync("/api/health");

        var id = response.Headers.GetValues("X-Request-Id").Single();
        if (expected is null)
        {
            id.Should().NotBe(incoming);
        }
        else
        {
            id.Should().Be(expected);
        }
    }
}
