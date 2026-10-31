using System.Net.Http.Headers;
using TripService.Application.Common.Interfaces;

namespace TripService.Infrastructure.Gateways;

public class BearerTokenHandler(IAccessTokenProvider tokens) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (tokens.Token is { Length: > 0 } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, ct);
    }
}