using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ApiGateway.Aggregation;

public static class AggregationEndpoints
{
    private sealed record Part(HttpStatusCode Status, JsonNode? Body);

    public static IEndpointRouteBuilder MapAggregationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/aggregate/trips/{id:guid}", GetTripWithPayment)
            .RequireAuthorization()
            .RequireRateLimiting("perClient");
        return app;
    }

    // Màn hình "chi tiết chuyến": chuyến + thanh toán trong một lời gọi, hai nguồn lấy song song
    private static async Task<IResult> GetTripWithPayment(
        Guid id, HttpRequest request, IHttpClientFactory factory, CancellationToken ct)
    {
        var auth = request.Headers.Authorization.ToString();   // chuyển tiếp token của người gọi
        var tripTask = FetchAsync(factory, "trip", $"/trips/{id}", auth, ct);
        var payTask = FetchAsync(factory, "payment", $"/payments/by-trip/{id}", auth, ct);
        var trip = await tripTask;
        var payment = await payTask;

        // Chuyến là thành phần chính: lỗi ở đây thì cả màn hình lỗi
        if (trip.Status != HttpStatusCode.OK)
            return trip.Status is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
                ? Results.StatusCode((int)trip.Status)
                : Results.Problem("Trip Service không khả dụng", statusCode: 503);

        // Thanh toán là thành phần phụ: lỗi thì vẫn trả chuyến, kèm lý do vắng mặt
        var paymentState = payment.Status switch
        {
            HttpStatusCode.OK => "found",
            HttpStatusCode.NotFound => "none",          // chưa có thanh toán
            HttpStatusCode.Forbidden => "hidden",       // người gọi không phải người trả tiền (ví dụ tài xế)
            _ => "unavailable"                          // Payment Service lỗi hoặc quá chậm
        };

        return Results.Ok(new
        {
            trip = trip.Body,
            payment = payment.Status == HttpStatusCode.OK ? payment.Body : null,
            paymentState
        });
    }

    private static async Task<Part> FetchAsync(
        IHttpClientFactory factory, string client, string path, string auth, CancellationToken ct)
    {
        try
        {
            using var msg = new HttpRequestMessage(HttpMethod.Get, path);
            if (!string.IsNullOrEmpty(auth)) msg.Headers.TryAddWithoutValidation("Authorization", auth);

            using var res = await factory.CreateClient(client).SendAsync(msg, ct);
            var body = res.StatusCode == HttpStatusCode.OK ? await res.Content.ReadFromJsonAsync<JsonNode>(ct) : null;
            return new Part(res.StatusCode, body);
        }
        catch (HttpRequestException) { return new Part(HttpStatusCode.ServiceUnavailable, null); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new Part(HttpStatusCode.GatewayTimeout, null); }
    }
}