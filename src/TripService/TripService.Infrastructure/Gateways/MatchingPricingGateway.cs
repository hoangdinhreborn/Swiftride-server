using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TripService.Application.Common.Exceptions;
using TripService.Application.Trips.Interfaces;
using TripService.Domain.Common;

namespace TripService.Infrastructure.Gateways;

public class MatchingPricingGateway(HttpClient http) : IPricingGateway
{
    // Mô hình riêng của Trip Service, không dùng chung code với Matching
    private sealed record QuoteBody(double PickupLat, double PickupLng, double DestinationLat, double DestinationLng, string? PromoCode);
    private sealed record QuoteResponse(decimal Total, decimal DistanceKm, decimal EstimatedMinutes, string Pricing);

    public async Task<QuoteResult> GetQuoteAsync(QuoteRequest r, CancellationToken ct = default)
    {
        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync("/quote",
                new QuoteBody(r.PickupLat, r.PickupLng, r.DestinationLat, r.DestinationLng, r.PromoCode), ct);
        }
        catch (HttpRequestException)
        {
            throw new PricingUnavailableException("Không kết nối được dịch vụ báo giá");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new PricingUnavailableException("Dịch vụ báo giá phản hồi quá chậm");   // hết timeout
        }

        using (res)
        {
            if (res.StatusCode == HttpStatusCode.BadRequest)   // ví dụ mã khuyến mãi sai
                throw new DomainValidationException(await ReadDetailAsync(res, ct));

            if (!res.IsSuccessStatusCode)   // 401/403 (sai token hoặc key), 5xx
                throw new PricingUnavailableException($"Dịch vụ báo giá lỗi ({(int)res.StatusCode})");

            var body = await res.Content.ReadFromJsonAsync<QuoteResponse>(ct)
                       ?? throw new PricingUnavailableException("Dịch vụ báo giá trả dữ liệu rỗng");

            return new QuoteResult(body.Total, body.DistanceKm, body.EstimatedMinutes, body.Pricing);
        }
    }

    private static async Task<string> ReadDetailAsync(HttpResponseMessage res, CancellationToken ct)
    {
        try
        {
            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("detail", out var d) && d.GetString() is { } s)
                return s;
        }
        catch (JsonException) { }
        return "Dịch vụ báo giá từ chối yêu cầu";
    }
}