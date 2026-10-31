using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MatchingService.Api.Auth;
using MatchingService.Application.Common.Security;

namespace MatchingService.Api.Endpoints;

public record DevTokenRequest(string Role, Guid? UserId = null);

public static class DevEndpoints
{
    // Chỉ map khi môi trường Development
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/dev/token", (DevTokenRequest req, IConfiguration cfg) =>
        {
            if (req.Role is not (Roles.Rider or Roles.Driver or Roles.Admin))
                return Results.BadRequest(new { error = "Role phải là Rider, Driver hoặc Admin" });

            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = cfg["Jwt:Issuer"],
                Audience = cfg["Jwt:Audience"],
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