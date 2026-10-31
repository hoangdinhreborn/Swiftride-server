namespace BuildingBlocks.Security;

public interface ICurrentUser
{
    Guid Id { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
