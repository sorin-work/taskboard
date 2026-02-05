using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Taskboard.Api.Features.Auth;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class StrictRateLimitFactory : TaskboardApiFactory
{
    protected override int AuthPermitsPerMinute => 3;
}

public class RateLimitTests(StrictRateLimitFactory factory) : IClassFixture<StrictRateLimitFactory>
{
    [Fact]
    public async Task Login_TooManyAttempts_Returns429()
    {
        var client = factory.CreateClient();
        var attempt = () => client.PostAsJsonAsync("/api/auth/login", new LoginRequest("x@example.com", "guess"));

        for (var i = 0; i < 3; i++)
        {
            (await attempt()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var blocked = await attempt();
        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        blocked.Headers.RetryAfter.Should().NotBeNull();
        (await blocked.ReadAsAsync<ProblemDetails>()).Title.Should().Be("Too many requests");
    }

    [Fact]
    public async Task OtherEndpoints_AreNotLimited()
    {
        var client = factory.CreateClient();

        for (var i = 0; i < 6; i++)
        {
            (await client.GetAsync("/api/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
