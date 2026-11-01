namespace PaymentService.Application.Payments;

// Phương thức thanh toán mặc định. Thực tế: tra theo từng rider
public sealed class AutoPayOptions
{
    public string Method { get; init; } = "Card";
    public string Token { get; init; } = "tok_default";
}