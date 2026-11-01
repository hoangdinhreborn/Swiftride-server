namespace TripService.Application.Trips;

public sealed class OfferExpiryOptions
{
    public TimeSpan Ttl { get; init; } = TimeSpan.FromSeconds(30);            // tài xế có bao lâu để trả lời
    public TimeSpan ScanInterval { get; init; } = TimeSpan.FromSeconds(5);    // worker quét bao lâu một lần
    public int BatchSize { get; init; } = 20;
}