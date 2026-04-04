using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Taskboard.Api.Domain;
using Taskboard.Api.Features.Auth;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class PasswordCheckTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private (HttpClient Client, IPasswordHasher<User> Hasher) ClientWithSpyHasher()
    {
        var real = new PasswordHasher<User>();
        var hasher = Substitute.For<IPasswordHasher<User>>();
        hasher.HashPassword(Arg.Any<User>(), Arg.Any<string>()).Returns(c => real.HashPassword(c.ArgAt<User>(0), c.ArgAt<string>(1)));
        hasher.VerifyHashedPassword(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(c => real.VerifyHashedPassword(c.ArgAt<User>(0), c.ArgAt<string>(1), c.ArgAt<string>(2)));

        var client = factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton(hasher))))
            .CreateClient();
        return (client, hasher);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_StillChecksAPassword()
    {
        var (client, hasher) = ClientWithSpyHasher();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ghost@example.com", "whatever-it-is"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        hasher.Received(1).VerifyHashedPassword(Arg.Any<User>(), Arg.Any<string>(), "whatever-it-is");
    }

    [Fact]
    public async Task Login_WhenHashIsOutdated_StoresANewHash()
    {
        var (client, hasher) = ClientWithSpyHasher();
        var email = $"rehash-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/signup", new SignupRequest(email, TestClients.Password, "Old Hash"));
        var before = factory.WithDb(db => db.Users.Single(u => u.Email == email).PasswordHash);
        hasher.VerifyHashedPassword(Arg.Is<User>(u => u.Email == email), before, TestClients.Password)
            .Returns(PasswordVerificationResult.SuccessRehashNeeded);

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, TestClients.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.WithDb(db => db.Users.Single(u => u.Email == email).PasswordHash).Should().NotBe(before);
    }
}
