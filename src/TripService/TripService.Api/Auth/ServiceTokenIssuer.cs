using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TripService.Api.Auth;

// Token cho các việc chạy nền (không có request nào của người dùng để chuyển tiếp token)
public sealed class ServiceTokenIssuer(IConfiguration cfg)
{
    private static readonly Guid SystemId = Guid.Parse("00000000-0000-0000-0000-00000000a001");
    private readonly object _gate = new();
    private string? _token;
    private DateTime _expiresAt;

    public string Issue()
    {
        lock (_gate)
        {
            if (_token is null || _expiresAt - DateTime.UtcNow < TimeSpan.FromMinutes(1))
            {
                _expiresAt = DateTime.UtcNow.AddMinutes(10);
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    cfg["Jwt:Key"] ?? throw new InvalidOperationException("Thiếu Jwt:Key")));

                _token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = cfg["Jwt:Issuer"],
                    Audience = cfg["Jwt:Audience"],
                    Subject = new ClaimsIdentity(
                    [
                        new Claim("sub", SystemId.ToString()),
                        new Claim("role", "Admin")
                    ]),
                    Expires = _expiresAt,
                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                });
            }
            return _token;
        }
    }
}