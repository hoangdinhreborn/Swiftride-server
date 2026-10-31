namespace PaymentService.Application.Common.Exceptions;

public class ConcurrencyException(string message) : Exception(message);