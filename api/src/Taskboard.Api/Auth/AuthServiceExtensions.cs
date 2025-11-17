using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Taskboard.Api.Domain;

namespace Taskboard.Api.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddTaskboardAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(JwtOptions.SectionName);
        var jwt = section.Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 characters long.");
        }

        services.Configure<JwtOptions>(section);
        services.AddSingleton<TokenService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });
        services.AddAuthorization();

        return services;
    }
}
