namespace PaymentService.Domain.Payments;

public class LedgerEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PaymentId { get; private set; }
    public LedgerAccount Account { get; private set; }
    public EntryDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private LedgerEntry() { }

    internal static LedgerEntry Create(Guid paymentId, LedgerAccount account, EntryDirection direction, decimal amount)
        => new() { PaymentId = paymentId, Account = account, Direction = direction, Amount = amount };
}