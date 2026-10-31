namespace TripService.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid Id { get; }
    string Role { get; }
}