using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;

namespace TripService.Infrastructure.Messaging;

internal static class MessagingTelemetry
{
    public const string SourceName = "SwiftRide.Trip.Messaging";
    public const string TraceParentHeader = "traceparent";
    public static readonly ActivitySource Source = new(SourceName);

    // Nối span consumer vào đúng trace của bên phát (đọc header traceparent)
    public static Activity? StartConsumer(string routingKey, IReadOnlyBasicProperties props)
    {
        ActivityContext parent = default;
        if (props.Headers is { } h && h.TryGetValue(TraceParentHeader, out var v) && v is byte[] raw)
            ActivityContext.TryParse(Encoding.UTF8.GetString(raw), null, out parent);

        var activity = Source.StartActivity($"{routingKey} process", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", routingKey);
        return activity;
    }
}