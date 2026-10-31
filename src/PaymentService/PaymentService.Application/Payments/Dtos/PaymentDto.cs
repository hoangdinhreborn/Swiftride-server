namespace PaymentService.Application.Payments.Dtos;

public record LedgerEntryDto(string Account, string Direction, decimal Amount);

public record PaymentDto(
    Guid Id, Guid TripId, Guid PayerId, decimal Amount, string Status,
    string? FailureReason, DateTime CreatedAt, IReadOnlyList<LedgerEntryDto> Ledger);