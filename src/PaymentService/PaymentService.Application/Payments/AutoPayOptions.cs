namespace PaymentService.Application.Payments;

// Token của phương thức thanh toán mặc định. Thực tế: tra theo từng rider
public sealed class AutoPayOptions
{
    public string Token { get; init; } = "tok_default";
}