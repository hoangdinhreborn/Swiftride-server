using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Trips;

namespace TripService.Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Pickup).HasMaxLength(300);
        b.Property(x => x.Destination).HasMaxLength(300);
        b.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        b.Property(x => x.QuotedFare).HasPrecision(18, 2);

        // Danh sách tài xế đã từ chối: cột uuid[] ánh xạ vào field, mặc định rỗng cho các dòng cũ
        b.Property<List<Guid>>("_declined").HasColumnName("DeclinedDriverIds").HasDefaultValueSql("'{}'");
        b.Ignore(x => x.DeclinedDriverIds);
        b.Ignore(x => x.DeclineCount);
        b.HasIndex(x => x.OfferedAt).HasFilter("\"Status\" = 'DriverOffered'");
    }
}