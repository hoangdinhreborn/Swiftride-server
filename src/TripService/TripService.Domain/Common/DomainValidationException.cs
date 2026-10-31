namespace TripService.Domain.Common;

public class DomainValidationException(string message) : DomainException(message);