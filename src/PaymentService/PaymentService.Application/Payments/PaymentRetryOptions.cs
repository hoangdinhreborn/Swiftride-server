namespace PaymentService.Application.Payments;

public sealed class PaymentRetryOptions
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(500);
}