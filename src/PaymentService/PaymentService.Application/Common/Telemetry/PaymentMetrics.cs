using System.Diagnostics.Metrics;

namespace PaymentService.Application.Common.Telemetry;

public static class PaymentMetrics
{
    private static readonly Meter Meter = new("SwiftRide.Payment");
    private static readonly Counter<long> Processed =
        Meter.CreateCounter<long>("swiftride.payments.processed", description: "Số thanh toán đã có kết quả");

    public static void Record(string outcome)
        => Processed.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}