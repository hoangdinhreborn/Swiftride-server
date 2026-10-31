namespace MatchingService.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid Id { get; }
    string Role { get; }
}