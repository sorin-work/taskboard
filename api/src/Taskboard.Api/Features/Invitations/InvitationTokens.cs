using System.Security.Cryptography;
using System.Text;

namespace Taskboard.Api.Features.Invitations;

public static class InvitationTokens
{
    /// <summary>Creates a URL-safe random token and the hash we keep in the database.</summary>
    public static (string Token, string Hash) Generate()
    {
        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
