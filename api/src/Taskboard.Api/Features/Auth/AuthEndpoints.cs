using System.Security.Claims;
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
        group.MapPost("/login", LoginAsync);
        group.MapGet("/me", GetMeAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> SignupAsync(
        SignupRequest request,
        TaskboardDbContext db,
        IPasswordHasher<User> hasher,
        TokenService tokens,
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

        return Results.Created("/api/auth/me", new AuthResponse(tokens.CreateToken(user), UserDto.From(user)));
    }

    private static async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        TaskboardDbContext db,
        IPasswordHasher<User> hasher,
        TokenService tokens,
        CancellationToken ct)
    {
        var email = EmailAddress.Normalize(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return new AuthResponse(tokens.CreateToken(user), UserDto.From(user));
    }

    private static async Task<UserDto> GetMeAsync(ClaimsPrincipal principal, TaskboardDbContext db, CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UnauthorizedException("Your account no longer exists.");
        return UserDto.From(user);
    }
}
