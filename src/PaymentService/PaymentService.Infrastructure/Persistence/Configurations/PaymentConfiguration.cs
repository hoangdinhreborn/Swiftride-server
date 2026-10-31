using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Payments;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IdempotencyKey).HasMaxLength(Payment.MaxKeyLength).IsRequired();
        b.Property(x => x.ProviderReference).HasMaxLength(100);
        b.Property(x => x.FailureReason).HasMaxLength(300);

        // Cùng một người, cùng một key: chỉ một thanh toán
        b.HasIndex(x => new { x.PayerId, x.IdempotencyKey }).IsUnique();
        // Mỗi chuyến chỉ một thanh toán chưa thất bại
        b.HasIndex(x => x.TripId).IsUnique().HasFilter("\"Status\" <> 'Failed'");

        // Chặn hai request cùng sửa một thanh toán (ví dụ hoàn tiền hai lần)
        b.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        b.HasMany(x => x.Entries).WithOne().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();   // quan trọng: để EF coi bút toán mới là Added, không phải Modified
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Account).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10);
    }
}