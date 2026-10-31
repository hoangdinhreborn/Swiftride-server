using System.Diagnostics.Metrics;

namespace TripService.Application.Common.Telemetry;

public static class TripMetrics
{
    private static readonly Meter Meter = new("SwiftRide.Trip");
    private static readonly Counter<long> Requested =
        Meter.CreateCounter<long>("swiftride.trips.requested", description: "Số lần đặt xe, theo kết quả");

    public static void TripRequested(string outcome)
        => Requested.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}