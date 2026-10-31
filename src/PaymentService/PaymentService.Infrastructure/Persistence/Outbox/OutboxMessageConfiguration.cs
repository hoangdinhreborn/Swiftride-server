using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PaymentService.Infrastructure.Persistence.Outbox;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.RoutingKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.Type).HasMaxLength(200).IsRequired();
        b.Property(x => x.Payload).IsRequired();
        b.Property(x => x.LastError).HasMaxLength(500);
        b.Property(x => x.TraceParent).HasMaxLength(60);

        // Chỉ index các dòng chưa gửi: worker quét nhanh dù bảng lớn
        b.HasIndex(x => x.CreatedAt).HasFilter("\"ProcessedAt\" IS NULL");
    }
}