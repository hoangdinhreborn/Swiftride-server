using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Security;

public record DevTokenRequest(string Role, Guid? UserId = null);

public static class SecurityExtensions
{
    public static SymmetricSecurityKey GetSigningKey(this IConfiguration cfg)
        => new(Encoding.UTF8.GetBytes(
            cfg["Jwt:Key"] ?? throw new InvalidOperationException("Thiếu Jwt:Key, hãy cấu hình qua dotnet user-secrets hoặc biến môi trường")));

    public static IServiceCollection AddSwiftRideAuth(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = cfg["Jwt:Issuer"] ?? "SwiftRide",
                    ValidAudience = cfg["Jwt:Audience"] ?? "SwiftRide",
                    IssuerSigningKey = cfg.GetSigningKey(),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.Zero
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

    public static IEndpointRouteBuilder MapDevAuthEndpoints(this IEndpointRouteBuilder app, IConfiguration cfg)
    {
        app.MapPost("/dev/token", (DevTokenRequest req) =>
        {
            if (req.Role is not (Roles.Rider or Roles.Driver or Roles.Admin))
                return Results.BadRequest(new { error = "Role phải là Rider, Driver hoặc Admin" });

            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = cfg["Jwt:Issuer"] ?? "SwiftRide",
                Audience = cfg["Jwt:Audience"] ?? "SwiftRide",
                Subject = new ClaimsIdentity(
                [
                    new Claim("sub", (req.UserId ?? Guid.NewGuid()).ToString()),
                    new Claim("role", req.Role)
                ]),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(cfg.GetSigningKey(), SecurityAlgorithms.HmacSha256)
            });

            return Results.Text(token);
        });

        return app;
    }
}
