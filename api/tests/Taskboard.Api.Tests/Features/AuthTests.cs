using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Taskboard.Api.Features.Auth;
using Taskboard.Api.Tests.Infrastructure;

namespace Taskboard.Api.Tests.Features;

public class AuthTests(TaskboardApiFactory factory) : IClassFixture<TaskboardApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Signup_ReturnsTokenAndUser()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/signup",
            new SignupRequest("Ada@Example.com", TestClients.Password, "Ada"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.ReadAsAsync<AuthResponse>();
        body.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be("ada@example.com");
        body.User.DisplayName.Should().Be("Ada");
    }

    [Fact]
    public async Task Signup_WithTakenEmail_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignupRequest("taken@example.com", TestClients.Password, "One"));

        var response = await _client.PostAsJsonAsync("/api/auth/signup",
            new SignupRequest("TAKEN@example.com ", TestClients.Password, "Two"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.ReadAsAsync<ProblemDetails>();
        problem.Detail.Should().Contain("already exists");
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignupRequest("login@example.com", TestClients.Password, "Lin"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("Login@example.com", TestClients.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<AuthResponse>();
        body.User.Email.Should().Be("login@example.com");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignupRequest("wrong@example.com", TestClients.Password, "Wes"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("wrong@example.com", "not-the-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@example.com", TestClients.Password));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsCurrentUser()
    {
        var (client, user) = await factory.CreateUserAsync("me");

        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me", TestJson.Options);

        me.Should().Be(user);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithGarbageToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("not-an-email", "long-enough-password", "Name", "Email")]
    [InlineData("short@example.com", "short", "Name", "Password")]
    [InlineData("noname@example.com", "long-enough-password", "", "DisplayName")]
    public async Task Signup_WithInvalidInput_ReturnsValidationProblem(string email, string password, string name, string field)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/signup", new SignupRequest(email, password, name));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadAsAsync<ValidationProblemDetails>();
        problem.Errors.Should().ContainKey(field);
    }

    [Fact]
    public async Task UpdateMe_ChangesDisplayName()
    {
        var (client, _) = await factory.CreateUserAsync("before");

        var response = await client.PatchAsJsonAsync("/api/auth/me", new UpdateProfileRequest("  After  "));

        (await response.ReadAsAsync<UserDto>()).DisplayName.Should().Be("After");
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_AllowsLoginWithNewOne()
    {
        var (client, user) = await factory.CreateUserAsync();

        var response = await client.PostAsJsonAsync("/api/auth/me/password",
            new ChangePasswordRequest(TestClients.Password, "a-brand-new-password"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, TestClients.Password)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, "a-brand-new-password")))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        var (client, _) = await factory.CreateUserAsync();

        var response = await client.PostAsJsonAsync("/api/auth/me/password",
            new ChangePasswordRequest("not-it", "a-brand-new-password"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
