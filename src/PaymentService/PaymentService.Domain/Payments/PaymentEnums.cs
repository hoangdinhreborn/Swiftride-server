namespace PaymentService.Domain.Payments;

public enum PaymentStatus { Pending, Succeeded, Failed, Refunded }

public enum LedgerAccount { GatewayClearing, TripRevenue }

public enum EntryDirection { Debit, Credit }