using System.Net;
using System.Net.Http.Json;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Payments.Interfaces;

namespace PaymentService.Infrastructure.Gateways;

public class TripHttpGateway(HttpClient http) : ITripGateway
{
    private sealed record TripBody(Guid Id, Guid RiderId, string Status, decimal QuotedFare);

    public async Task<TripInfo?> GetTripAsync(Guid tripId, CancellationToken ct = default)
    {
        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync($"/trips/{tripId}", ct);
        }
        catch (HttpRequestException)
        {
            throw new DependencyUnavailableException("Không kết nối được Trip Service");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new DependencyUnavailableException("Trip Service phản hồi quá chậm");
        }

        using (res)
        {
            if (res.StatusCode == HttpStatusCode.NotFound) return null;
            if (res.StatusCode == HttpStatusCode.Forbidden)
                throw new ForbiddenException("Bạn không có quyền với chuyến này");
            if (!res.IsSuccessStatusCode)   // 401 (sai token/key) hoặc 5xx
                throw new DependencyUnavailableException($"Trip Service lỗi ({(int)res.StatusCode})");

            var body = await res.Content.ReadFromJsonAsync<TripBody>(ct)
                       ?? throw new DependencyUnavailableException("Trip Service trả dữ liệu rỗng");
            return new TripInfo(body.Id, body.RiderId, body.Status, body.QuotedFare);
        }
    }
}