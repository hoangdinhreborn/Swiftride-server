using System.Diagnostics.Metrics;

namespace PaymentService.Infrastructure.Messaging;

internal static class OutboxMetrics
{
    private static int _pending;
    private static readonly Meter Meter = new("SwiftRide.Payment.Outbox");

    // Số sự kiện đã ghi nhưng chưa gửi được lên broker
    private static readonly ObservableGauge<int> Pending =
        Meter.CreateObservableGauge("swiftride.outbox.pending", () => Volatile.Read(ref _pending),
            description: "Số dòng outbox chưa gửi");

    public static void SetPending(int n) => Volatile.Write(ref _pending, n);
}