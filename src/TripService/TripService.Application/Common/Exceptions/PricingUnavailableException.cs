namespace TripService.Application.Common.Exceptions;

public class PricingUnavailableException(string message) : Exception(message);