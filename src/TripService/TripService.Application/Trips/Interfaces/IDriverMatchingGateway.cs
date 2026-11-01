namespace TripService.Application.Trips.Interfaces;

public record MatchRequest(double PickupLat, double PickupLng, IReadOnlyCollection<Guid> ExcludeDriverIds);

public interface IDriverMatchingGateway
{
    // Tài xế rảnh gần nhất (không nằm trong danh sách loại trừ), null nếu không có
    Task<Guid?> FindNearestDriverAsync(MatchRequest request, CancellationToken ct = default);
}