using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace MatchingService.Infrastructure.Persistence.Documents;

// Mô hình lưu trữ riêng, để Domain không dính attribute của MongoDB
public class DriverLocationDocument
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid DriverId { get; set; }

    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public DateTime UpdatedAt { get; set; }
}