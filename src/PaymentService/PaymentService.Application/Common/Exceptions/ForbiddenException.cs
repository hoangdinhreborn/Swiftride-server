namespace PaymentService.Application.Common.Exceptions;

public class ForbiddenException(string message) : Exception(message);