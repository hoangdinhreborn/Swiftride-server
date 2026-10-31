using MatchingService.Infrastructure.Persistence.Documents;
using MongoDB.Driver;

namespace MatchingService.Infrastructure.Persistence;

public class MatchingMongoContext
{
    public IMongoCollection<DriverLocationDocument> DriverLocations { get; }

    public MatchingMongoContext(IMongoDatabase db)
    {
        DriverLocations = db.GetCollection<DriverLocationDocument>("driver_locations");

        // Index 2dsphere để truy vấn $near. Tạo lại nhiều lần vẫn an toàn
        DriverLocations.Indexes.CreateOne(new CreateIndexModel<DriverLocationDocument>(
            Builders<DriverLocationDocument>.IndexKeys.Geo2DSphere(x => x.Location)));
    }
}