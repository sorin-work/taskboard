namespace Taskboard.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC signing key. Must be at least 32 characters.</summary>
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "taskboard";

    public string Audience { get; set; } = "taskboard";

    public int LifetimeHours { get; set; } = 168;
}
