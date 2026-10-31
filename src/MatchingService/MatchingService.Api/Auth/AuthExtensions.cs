using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MatchingService.Application.Common.Interfaces;
using MatchingService.Application.Common.Security;

namespace MatchingService.Api.Auth;

public static class AuthExtensions
{
    public static SymmetricSecurityKey GetSigningKey(this IConfiguration cfg)
        => new(Encoding.UTF8.GetBytes(
            cfg["Jwt:Key"] ?? throw new InvalidOperationException("Thiếu Jwt:Key, hãy chạy dotnet user-secrets set")));

    public static IServiceCollection AddApiAuth(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = cfg["Jwt:Issuer"],
                    ValidAudience = cfg["Jwt:Audience"],
                    IssuerSigningKey = cfg.GetSigningKey(),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.RiderOnly,  p => p.RequireRole(Roles.Rider))
            .AddPolicy(Policies.DriverOnly, p => p.RequireRole(Roles.Driver))
            .AddPolicy(Policies.AnyRole,    p => p.RequireRole(Roles.Rider, Roles.Driver, Roles.Admin));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }
}