using PaymentService.Application.Common.Interfaces;

namespace PaymentService.Api.Auth;

public class HttpContextAccessTokenProvider(IHttpContextAccessor accessor) : IAccessTokenProvider
{
    private const string Prefix = "Bearer ";

    public string? Token
    {
        get
        {
            var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
            return header is not null && header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                ? header[Prefix.Length..].Trim()
                : null;
        }
    }
}