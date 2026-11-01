using PaymentService.Domain.Common;

namespace PaymentService.Domain.Payments;

public enum PaymentMethodKind { Card, Wallet }

// Chỉ giữ token do cổng thanh toán cấp, không bao giờ giữ số thẻ hay CVV
public abstract class PaymentMethod
{
    public const int MaxTokenLength = 100;

    public string Token { get; }
    public abstract PaymentMethodKind Kind { get; }
    protected abstract string TokenPrefix { get; }

    protected PaymentMethod(string token)
    {
        var t = token?.Trim();
        if (string.IsNullOrEmpty(t))
            throw new DomainValidationException("Thiếu token thanh toán");
        if (t.Length > MaxTokenLength)
            throw new DomainValidationException($"Token thanh toán tối đa {MaxTokenLength} ký tự");
        if (!t.StartsWith(TokenPrefix, StringComparison.Ordinal))
            throw new DomainValidationException(
                $"Token của phương thức {Kind} phải bắt đầu bằng '{TokenPrefix}'. Hệ thống không nhận số thẻ");

        Token = t;
    }
}

public sealed class CardPaymentMethod(string token) : PaymentMethod(token)
{
    public override PaymentMethodKind Kind => PaymentMethodKind.Card;
    protected override string TokenPrefix => "tok_";
}

public sealed class WalletPaymentMethod(string token) : PaymentMethod(token)
{
    public override PaymentMethodKind Kind => PaymentMethodKind.Wallet;
    protected override string TokenPrefix => "wallet_";
}