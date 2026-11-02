using PaymentService.Domain.Common;

namespace PaymentService.Domain.Payments;

public class Payment
{
    public const int MaxKeyLength = 100;
    private readonly List<LedgerEntry> _entries = [];

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TripId { get; private set; }
    public Guid PayerId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public PaymentMethodKind Method { get; private set; } = PaymentMethodKind.Card;
    public string IdempotencyKey { get; private set; } = "";
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public string? RefundReason { get; private set; }
    public int Attempts { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public IReadOnlyList<LedgerEntry> Entries => _entries;

    private Payment() { }

    public static Payment Initiate(Guid tripId, Guid payerId, decimal amount, string idempotencyKey,
        PaymentMethodKind method = PaymentMethodKind.Card)
    {
        if (tripId == Guid.Empty) throw new DomainValidationException("Thiếu mã chuyến");
        if (payerId == Guid.Empty) throw new DomainValidationException("Thiếu người thanh toán");
        if (amount <= 0) throw new DomainValidationException("Số tiền phải lớn hơn 0");

        var key = idempotencyKey?.Trim();
        if (string.IsNullOrEmpty(key) || key.Length > MaxKeyLength)
            throw new DomainValidationException($"Idempotency-Key là bắt buộc, tối đa {MaxKeyLength} ký tự");

        return new() { TripId = tripId, PayerId = payerId, Amount = amount, IdempotencyKey = key, Method = method };
    }

    // Đếm số lần đã gọi cổng thanh toán
    public void RecordAttempt()
    {
        Require(PaymentStatus.Pending, PaymentStatus.Deferred);
        Attempts++;
    }

    // Rider đổi phương thức khi thử lại một thanh toán chưa có kết quả cuối
    public void UseMethod(PaymentMethodKind method)
    {
        Require(PaymentStatus.Pending, PaymentStatus.Deferred);
        Method = method;
    }

    public void MarkSucceeded(string providerReference)
    {
        Require(PaymentStatus.Pending, PaymentStatus.Deferred);
        Status = PaymentStatus.Succeeded;
        ProviderReference = providerReference;
        FailureReason = null;
        Post(debit: LedgerAccount.GatewayClearing, credit: LedgerAccount.TripRevenue);
    }

    public void MarkFailed(string reason)
    {
        Require(PaymentStatus.Pending, PaymentStatus.Deferred);
        Status = PaymentStatus.Failed;
        FailureReason = Truncate(reason);
    }

    // Lỗi tạm thời và hết lượt thử: giữ ở trạng thái "chờ xử lý", không ghi sổ cái
    public void MarkDeferred(string reason)
    {
        Require(PaymentStatus.Pending, PaymentStatus.Deferred);
        Status = PaymentStatus.Deferred;
        FailureReason = Truncate(reason);
    }

    public void Refund(string? reason = null)
    {
        Require(PaymentStatus.Succeeded);
        Status = PaymentStatus.Refunded;
        RefundReason = reason is null ? null : Truncate(reason);
        Post(debit: LedgerAccount.TripRevenue, credit: LedgerAccount.GatewayClearing);   // đảo bút toán
    }

    private void Post(LedgerAccount debit, LedgerAccount credit)
    {
        _entries.Add(LedgerEntry.Create(Id, debit, EntryDirection.Debit, Amount));
        _entries.Add(LedgerEntry.Create(Id, credit, EntryDirection.Credit, Amount));
    }

    private void Require(params PaymentStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new DomainException(
                $"Thanh toán đang ở trạng thái {Status}, cần {string.Join(" hoặc ", allowed)}");
    }

    private static string Truncate(string s) => s.Length > 300 ? s[..300] : s;
}