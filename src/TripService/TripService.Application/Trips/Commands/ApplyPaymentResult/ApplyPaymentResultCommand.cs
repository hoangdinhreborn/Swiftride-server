namespace TripService.Application.Trips.Commands.ApplyPaymentResult;

public record ApplyPaymentResultCommand(Guid TripId, bool Succeeded);