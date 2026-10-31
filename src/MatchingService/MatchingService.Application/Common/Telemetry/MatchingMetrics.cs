using System.Diagnostics.Metrics;

namespace MatchingService.Application.Common.Telemetry;

public static class MatchingMetrics
{
    private static readonly Meter Meter = new("SwiftRide.Matching");
    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("swiftride.matching.duration", unit: "ms", description: "Thời gian tìm tài xế");

    public static void Record(double milliseconds, bool found)
        => Duration.Record(milliseconds, new KeyValuePair<string, object?>("found", found));
}