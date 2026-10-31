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
    public string IdempotencyKey { get; private set; } = "";
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public IReadOnlyList<LedgerEntry> Entries => _entries;

    private Payment() { }

    public static Payment Initiate(Guid tripId, Guid payerId, decimal amount, string idempotencyKey)
    {
        if (tripId == Guid.Empty) throw new DomainValidationException("Thiếu mã chuyến");
        if (payerId == Guid.Empty) throw new DomainValidationException("Thiếu người thanh toán");
        if (amount <= 0) throw new DomainValidationException("Số tiền phải lớn hơn 0");

        var key = idempotencyKey?.Trim();
        if (string.IsNullOrEmpty(key) || key.Length > MaxKeyLength)
            throw new DomainValidationException($"Idempotency-Key là bắt buộc, tối đa {MaxKeyLength} ký tự");

        return new() { TripId = tripId, PayerId = payerId, Amount = amount, IdempotencyKey = key };
    }

    public void MarkSucceeded(string providerReference)
    {
        Require(PaymentStatus.Pending);
        Status = PaymentStatus.Succeeded;
        ProviderReference = providerReference;
        Post(debit: LedgerAccount.GatewayClearing, credit: LedgerAccount.TripRevenue);
    }

    public void MarkFailed(string reason)
    {
        Require(PaymentStatus.Pending);
        Status = PaymentStatus.Failed;
        FailureReason = reason.Length > 300 ? reason[..300] : reason;
    }

    public void Refund()
    {
        Require(PaymentStatus.Succeeded);
        Status = PaymentStatus.Refunded;
        Post(debit: LedgerAccount.TripRevenue, credit: LedgerAccount.GatewayClearing);   // đảo bút toán
    }

    private void Post(LedgerAccount debit, LedgerAccount credit)
    {
        _entries.Add(LedgerEntry.Create(Id, debit, EntryDirection.Debit, Amount));
        _entries.Add(LedgerEntry.Create(Id, credit, EntryDirection.Credit, Amount));
    }

    private void Require(PaymentStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Thanh toán đang ở trạng thái {Status}, cần {expected}");
    }
}