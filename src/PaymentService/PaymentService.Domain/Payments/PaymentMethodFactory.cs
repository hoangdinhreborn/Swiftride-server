using PaymentService.Domain.Common;

namespace PaymentService.Domain.Payments;

public static class PaymentMethodFactory
{
    // kind rỗng nghĩa là Card, để client cũ không gửi trường này vẫn chạy
    public static PaymentMethod Create(string? kind, string? token)
        => kind?.Trim().ToLowerInvariant() switch
        {
            null or "" or "card" => new CardPaymentMethod(token!),
            "wallet"             => new WalletPaymentMethod(token!),
            _ => throw new DomainValidationException($"Phương thức thanh toán '{kind}' không được hỗ trợ (Card hoặc Wallet)")
        };
}