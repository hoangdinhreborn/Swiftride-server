using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddSwiftRideTelemetry(this WebApplicationBuilder builder, string serviceName)
    {
        // Console hiển thị scope để thấy TraceId/SpanId cạnh mỗi dòng log
        builder.Logging.AddSimpleConsole(o => { o.IncludeScopes = true; o.SingleLine = true; });

        builder.Services.Configure<OpenTelemetryLoggerOptions>(o =>
        {
            o.IncludeFormattedMessage = true;
            o.IncludeScopes = true;
        });

        var endpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://localhost:4317";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()      // gồm cả request YARP chuyển tiếp và Trip gọi Matching
                .AddSource("SwiftRide.*"))           // span RabbitMQ do từng service tự khai báo
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()      // http.server.request.duration = độ trễ API
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("SwiftRide.*"))            // metric nghiệp vụ do từng service tự khai báo
            .WithLogging()
            .UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(endpoint));

        return builder;
    }

    // Trả TraceId về client qua header. Dùng làm CorrelationId khi cần tìm trace hoặc log
    public static IApplicationBuilder UseTraceIdHeader(this IApplicationBuilder app)
        => app.Use(async (ctx, next) =>
        {
            var traceId = Activity.Current?.TraceId.ToString();
            if (traceId is not null)
                ctx.Response.OnStarting(() =>
                {
                    ctx.Response.Headers["X-Trace-Id"] = traceId;
                    return Task.CompletedTask;
                });
            await next();
        });
}