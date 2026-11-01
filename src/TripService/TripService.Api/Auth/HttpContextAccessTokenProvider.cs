using TripService.Application.Common.Interfaces;

namespace TripService.Api.Auth;

public class HttpContextAccessTokenProvider(IHttpContextAccessor accessor, ServiceTokenIssuer serviceTokens)
    : IAccessTokenProvider
{
    private const string Prefix = "Bearer ";

    public string? Token
    {
        get
        {
            var ctx = accessor.HttpContext;
            if (ctx is null) return serviceTokens.Issue();   // việc chạy nền: dùng token dịch vụ

            var header = ctx.Request.Headers.Authorization.ToString();
            return header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                ? header[Prefix.Length..].Trim()
                : null;
        }
    }
}