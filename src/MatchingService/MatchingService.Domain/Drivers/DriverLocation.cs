using MatchingService.Domain.Common;
using MatchingService.Domain.Geo;

namespace MatchingService.Domain.Drivers;

public class DriverLocation
{
    public Guid DriverId { get; private set; }
    public GeoPoint Location { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private DriverLocation() { }

    public static DriverLocation Create(Guid driverId, GeoPoint location, bool isAvailable)
    {
        if (driverId == Guid.Empty)
            throw new DomainValidationException("Thiếu thông tin tài xế");

        return new()
        {
            DriverId = driverId,
            Location = location,
            IsAvailable = isAvailable,
            UpdatedAt = DateTime.UtcNow
        };
    }
}