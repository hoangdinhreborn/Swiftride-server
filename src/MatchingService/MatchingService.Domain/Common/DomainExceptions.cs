namespace MatchingService.Domain.Common;

public class DomainException(string message) : Exception(message);

public class DomainValidationException(string message) : DomainException(message);