namespace TripService.Application.Common.Interfaces;

public interface IAccessTokenProvider
{
    string? Token { get; }
}