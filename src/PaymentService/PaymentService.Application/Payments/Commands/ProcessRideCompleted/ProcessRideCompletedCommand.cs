namespace PaymentService.Application.Payments.Commands.ProcessRideCompleted;

public record ProcessRideCompletedCommand(Guid EventId, Guid TripId, Guid RiderId, decimal Amount);