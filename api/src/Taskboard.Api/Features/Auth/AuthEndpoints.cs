using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taskboard.Api.Auth;
using Taskboard.Api.Common;
using Taskboard.Api.Data;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/signup", SignupAsync);

        return app;
    }

    private static async Task<IResult> SignupAsync(
        SignupRequest request,
        TaskboardDbContext db,
        IPasswordHasher<User> hasher,
        TimeProvider clock,
        CancellationToken ct)
    {
        var email = EmailAddress.Normalize(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return Results.Created("/api/auth/me", UserDto.From(user));
    }
}
