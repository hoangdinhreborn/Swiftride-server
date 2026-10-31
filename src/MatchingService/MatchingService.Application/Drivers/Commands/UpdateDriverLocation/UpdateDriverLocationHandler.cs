using MatchingService.Application.Common.Interfaces;
using MatchingService.Application.Drivers.Dtos;
using MatchingService.Application.Drivers.Interfaces;
using MatchingService.Domain.Drivers;
using MatchingService.Domain.Geo;

namespace MatchingService.Application.Drivers.Commands.UpdateDriverLocation;

public sealed class UpdateDriverLocationHandler(IDriverLocationRepository repo, ICurrentUser user)
    : IHandler<UpdateDriverLocationCommand, DriverLocationDto>
{
    public async Task<DriverLocationDto> HandleAsync(UpdateDriverLocationCommand cmd, CancellationToken ct = default)
    {
        var point = GeoPoint.From(cmd.Lat, cmd.Lng, "tài xế");
        var driver = DriverLocation.Create(user.Id, point, cmd.IsAvailable);   // DriverId lấy từ token

        await repo.UpsertAsync(driver, ct);

        return new DriverLocationDto(driver.DriverId, point.Latitude, point.Longitude,
            driver.IsAvailable, driver.UpdatedAt);
    }
}