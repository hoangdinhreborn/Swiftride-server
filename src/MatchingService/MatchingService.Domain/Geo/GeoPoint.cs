using MatchingService.Domain.Common;

namespace MatchingService.Domain.Geo;

public readonly record struct GeoPoint
{
    private const double EarthRadiusKm = 6371.0;

    public double Latitude { get; }
    public double Longitude { get; }

    public GeoPoint(double latitude, double longitude)
    {
        // Viết kiểu !(a && b) để NaN cũng bị từ chối
        if (!(latitude >= -90 && latitude <= 90))
            throw new DomainValidationException("Vĩ độ phải nằm trong khoảng -90 đến 90");
        if (!(longitude >= -180 && longitude <= 180))
            throw new DomainValidationException("Kinh độ phải nằm trong khoảng -180 đến 180");

        Latitude = latitude;
        Longitude = longitude;
    }

    public static GeoPoint From(double? latitude, double? longitude, string name)
    {
        if (latitude is null || longitude is null)
            throw new DomainValidationException($"Thiếu tọa độ {name}");
        return new GeoPoint(latitude.Value, longitude.Value);
    }

    // Haversine: khoảng cách đường chim bay (km)
    public double DistanceKmTo(GeoPoint other)
    {
        var dLat = ToRad(other.Latitude - Latitude);
        var dLon = ToRad(other.Longitude - Longitude);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
              + Math.Cos(ToRad(Latitude)) * Math.Cos(ToRad(other.Latitude)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180;
}