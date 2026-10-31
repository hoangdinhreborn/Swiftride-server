using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Security;

public class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ClaimsPrincipal? User => http.HttpContext?.User;

    public Guid Id
    {
        get
        {
            var raw = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User?.FindFirst("sub")?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value
                        ?? User?.FindFirst("role")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}
