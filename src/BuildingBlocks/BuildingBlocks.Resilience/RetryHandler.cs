using System.Net;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Resilience;

public sealed class RetryOptions
{
    public int MaxAttempts { get; init; } = 3;                                         // tổng số lần thử, kể cả lần đầu
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(2);          // thời gian chờ MỖI lần thử
}

// CHỈ gắn vào HttpClient mà mọi request đều an toàn khi lặp lại (đọc, hoặc có idempotency key)
public sealed class RetryHandler(RetryOptions options, ILogger? log = null) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Đưa body vào bộ nhớ để gửi lại được nhiều lần
        if (request.Content is not null) await request.Content.LoadIntoBufferAsync(ct);

        for (var attempt = 1; ; attempt++)
        {
            var last = attempt >= options.MaxAttempts;
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            attemptCts.CancelAfter(options.AttemptTimeout);

            string reason;
            try
            {
                var response = await base.SendAsync(request, attemptCts.Token);
                if (last || !IsTransient(response.StatusCode)) return response;   // thành công, lỗi dứt khoát, hoặc hết lượt

                reason = $"HTTP {(int)response.StatusCode}";
                response.Dispose();
            }
            catch (HttpRequestException ex) when (!last) { reason = ex.Message; }
            // Hết thời gian chờ của lần thử này (không phải người gọi huỷ) thì thử lại
            catch (OperationCanceledException) when (!last && !ct.IsCancellationRequested) { reason = "quá thời gian chờ"; }

            var delay = Backoff(attempt);
            log?.LogWarning("{Method} {Path}: lần {Attempt}/{Max} thất bại ({Reason}), thử lại sau {Delay:0} ms",
                request.Method, request.RequestUri?.AbsolutePath, attempt, options.MaxAttempts, reason, delay.TotalMilliseconds);
            await Task.Delay(delay, ct);
        }
    }

    private static bool IsTransient(HttpStatusCode code)
        => code is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)code >= 500;

    // Exponential backoff + jitter: base, 2·base, 4·base... cộng ngẫu nhiên tới 50% base
    private TimeSpan Backoff(int attempt)
    {
        var baseMs = options.BaseDelay.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(baseMs * Math.Pow(2, attempt - 1) + Random.Shared.NextDouble() * baseMs * 0.5);
    }
}