using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Auth;

public sealed record SignupRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record UpdateProfileRequest(string DisplayName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserDto(Guid Id, string Email, string DisplayName)
{
    public static UserDto From(User user) => new(user.Id, user.Email, user.DisplayName);
}

public sealed record AuthResponse(string Token, UserDto User);
