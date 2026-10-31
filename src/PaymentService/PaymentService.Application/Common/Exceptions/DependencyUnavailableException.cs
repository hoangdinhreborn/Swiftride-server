namespace PaymentService.Application.Common.Exceptions;

public class DependencyUnavailableException(string message) : Exception(message);