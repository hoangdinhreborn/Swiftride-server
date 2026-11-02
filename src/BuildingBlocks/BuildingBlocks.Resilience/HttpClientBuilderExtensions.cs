using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Resilience;

public static class HttpClientBuilderExtensions
{
    // Gọi TRƯỚC các handler khác: retry là lớp ngoài cùng, mỗi lần thử đi lại qua các handler bên trong
    public static IHttpClientBuilder AddSwiftRideRetry(this IHttpClientBuilder builder, RetryOptions? options = null)
    {
        var o = options ?? new RetryOptions();
        return builder.AddHttpMessageHandler(sp =>
            new RetryHandler(o, sp.GetRequiredService<ILoggerFactory>().CreateLogger<RetryHandler>()));
    }
}