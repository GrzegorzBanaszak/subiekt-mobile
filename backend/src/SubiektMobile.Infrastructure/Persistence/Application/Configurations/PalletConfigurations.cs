using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Domain.Orders;

namespace SubiektMobile.Infrastructure.Persistence.Application.Configurations;

public sealed class PalletConfiguration : IEntityTypeConfiguration<Pallet>
{
    public void Configure(EntityTypeBuilder<Pallet> builder)
    {
        builder.ToTable("pallets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.EmptyPalletWeightKg).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.GoodsWeightKg).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.TotalWeightKg).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ClosedByKind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ClosedByName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.OperationId).IsUnique().HasDatabaseName("IX_pallets_operation_id");
        builder.HasIndex(x => x.Number).IsUnique().HasDatabaseName("IX_pallets_number");
        builder.HasIndex(x => new { x.OrderId, x.ClosedAtUtc });
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.PalletId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PalletItemConfiguration : IEntityTypeConfiguration<PalletItem>
{
    public void Configure(EntityTypeBuilder<PalletItem> builder)
    {
        builder.ToTable("pallet_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.UnitWeightKg).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.LineWeightKg).HasPrecision(18, 4).IsRequired();
        builder.HasIndex(x => new { x.PalletId, x.OrderItemId }).IsUnique();
        builder.HasIndex(x => x.OrderItemId);
        builder.HasIndex(x => x.OrderId);
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
