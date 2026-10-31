using System.Security.Claims;
using TripService.Application.Common.Interfaces;

namespace TripService.Api.Auth;

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("Không có HttpContext");

    public Guid Id => Guid.Parse(Principal.FindFirstValue("sub")!);
    public string Role => Principal.FindFirstValue("role")!;
}