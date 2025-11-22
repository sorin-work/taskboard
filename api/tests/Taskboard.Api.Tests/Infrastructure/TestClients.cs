using System.Net.Http.Headers;
using System.Net.Http.Json;
using Taskboard.Api.Features.Auth;

namespace Taskboard.Api.Tests.Infrastructure;

public static class TestClients
{
    public const string Password = "correct-horse-battery";

    /// <summary>Signs up a fresh user and returns a client that sends their token.</summary>
    public static async Task<(HttpClient Client, UserDto User)> CreateUserAsync(this TaskboardApiFactory factory, string? name = null)
    {
        name ??= "user";
        var email = $"{name}-{Guid.NewGuid():N}@example.com";
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/signup", new SignupRequest(email, Password, name));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return (client, auth.User);
    }

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(TestJson.Options);
        return value ?? throw new InvalidOperationException("Response body was empty.");
    }
}
