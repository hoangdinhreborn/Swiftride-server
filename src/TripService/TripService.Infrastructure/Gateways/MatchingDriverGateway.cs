using System.Net.Http.Json;
using TripService.Application.Common.Exceptions;
using TripService.Application.Trips.Interfaces;

namespace TripService.Infrastructure.Gateways;

public class MatchingDriverGateway(HttpClient http) : IDriverMatchingGateway
{
    private sealed record MatchBody(double Lat, double Lng, IReadOnlyCollection<Guid> ExcludeDriverIds);
    private sealed record MatchItem(Guid DriverId);

    public async Task<Guid?> FindNearestDriverAsync(MatchRequest r, CancellationToken ct = default)
    {
        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync("/match", new MatchBody(r.PickupLat, r.PickupLng, r.ExcludeDriverIds), ct);
        }
        catch (HttpRequestException)
        {
            throw new PricingUnavailableException("Không kết nối được dịch vụ ghép xe");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new PricingUnavailableException("Dịch vụ ghép xe phản hồi quá chậm");
        }

        using (res)
        {
            if (!res.IsSuccessStatusCode)
                throw new PricingUnavailableException($"Dịch vụ ghép xe lỗi ({(int)res.StatusCode})");

            var items = await res.Content.ReadFromJsonAsync<List<MatchItem>>(ct) ?? [];
            return items.Count > 0 ? items[0].DriverId : null;   // Matching đã sắp theo khoảng cách tăng dần
        }
    }
}