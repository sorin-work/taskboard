namespace Taskboard.Api.Domain;

public class User
{
    public Guid Id { get; set; }

    /// <summary>Always stored lower-cased and trimmed.</summary>
    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
