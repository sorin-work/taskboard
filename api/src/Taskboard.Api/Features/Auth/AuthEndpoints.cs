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
    // Checked against when the email is unknown, so a miss takes as long as a wrong password.
    private static readonly Lazy<string> _dummyHash = new(() =>
        new PasswordHasher<User>().HashPassword(new User { Email = "", DisplayName = "" }, Guid.NewGuid().ToString()));

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/signup", SignupAsync).Validate<SignupRequest>().RequireRateLimiting(RateLimiting.AuthPolicy);
        group.MapPost("/login", LoginAsync).Validate<LoginRequest>().RequireRateLimiting(RateLimiting.AuthPolicy);
        group.MapGet("/me", GetMeAsync).RequireAuthorization();
        group.MapPatch("/me", UpdateMeAsync).RequireAuthorization().Validate<UpdateProfileRequest>();
        group.MapPost("/me/password", ChangePasswordAsync).RequireAuthorization().Validate<ChangePasswordRequest>();

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
        if (user is null)
        {
            hasher.VerifyHashedPassword(new User { Email = email, DisplayName = "" }, _dummyHash.Value, request.Password);
            throw new UnauthorizedException("Invalid email or password.");
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        // The hash was made with older settings (fewer iterations); upgrade it now we have the password.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(ct);
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

    private static async Task<UserDto> UpdateMeAsync(
        UpdateProfileRequest request,
        ClaimsPrincipal principal,
        TaskboardDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users.FindAsync([principal.GetUserId()], ct)
            ?? throw new UnauthorizedException("Your account no longer exists.");
        user.DisplayName = request.DisplayName.Trim();
        await db.SaveChangesAsync(ct);
        return UserDto.From(user);
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        TaskboardDbContext db,
        IPasswordHasher<User> hasher,
        CancellationToken ct)
    {
        var user = await db.Users.FindAsync([principal.GetUserId()], ct)
            ?? throw new UnauthorizedException("Your account no longer exists.");
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            throw new BadRequestException("Your current password is not correct.");
        }

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
