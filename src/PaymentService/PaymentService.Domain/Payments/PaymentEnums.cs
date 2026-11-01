namespace PaymentService.Domain.Payments;

// Deferred: cổng thanh toán lỗi tạm thời và đã hết lượt thử.
// Chưa biết tiền đã trừ hay chưa nên KHÔNG đánh dấu Failed
public enum PaymentStatus { Pending, Succeeded, Failed, Refunded, Deferred }

public enum LedgerAccount { GatewayClearing, TripRevenue }

public enum EntryDirection { Debit, Credit }