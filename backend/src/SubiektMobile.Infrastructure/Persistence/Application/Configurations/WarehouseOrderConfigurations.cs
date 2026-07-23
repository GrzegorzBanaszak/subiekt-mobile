using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Infrastructure.Persistence.Application.Configurations;

public sealed class WarehouseOrderConfiguration : IEntityTypeConfiguration<WarehouseOrder>
{
    public void Configure(EntityTypeBuilder<WarehouseOrder> builder)
    {
        builder.ToTable("warehouse_orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SubiektSourceDocumentNumber).HasMaxLength(40);
        builder.HasIndex(x => x.SubiektSourceDocumentId).IsUnique().HasFilter("\"SubiektSourceDocumentId\" IS NOT NULL");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.UpdatedByName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.PickingMode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.WarehouseOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Assignees).WithOne().HasForeignKey(x => x.WarehouseOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Assignees).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.Status, x.UpdatedAtUtc });
    }
}

public sealed class WarehouseOrderAssigneeConfiguration : IEntityTypeConfiguration<WarehouseOrderAssignee>
{
    public void Configure(EntityTypeBuilder<WarehouseOrderAssignee> builder)
    {
        builder.ToTable("warehouse_order_assignees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeDisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AssignedByName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.WarehouseOrderId, x.EmployeeId }).IsUnique();
        builder.HasIndex(x => x.EmployeeId);
        builder.HasOne<SubiektMobile.Domain.Identity.Employee>().WithMany()
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SubiektMobile.Domain.Identity.Organization>().WithMany()
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WarehouseOrderItemConfiguration : IEntityTypeConfiguration<WarehouseOrderItem>
{
    public void Configure(EntityTypeBuilder<WarehouseOrderItem> builder)
    {
        builder.ToTable("warehouse_order_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProductSymbol).HasMaxLength(50);
        builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UnitWeightKg).HasPrecision(18, 4);
        builder.HasIndex(x => x.SubiektSourceItemId).IsUnique().HasFilter("\"SubiektSourceItemId\" IS NOT NULL");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.ReservedByKind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ReservedByName).HasMaxLength(120);
        builder.Property(x => x.PackedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.PackedByKind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PackedByName).HasMaxLength(120);
        builder.HasIndex(x => new { x.WarehouseOrderId, x.ProductId });
        builder.HasIndex(x => new { x.WarehouseOrderId, x.Status });
    }
}

public sealed class WarehouseOrderPickingEventConfiguration : IEntityTypeConfiguration<WarehouseOrderPickingEvent>
{
    public void Configure(EntityTypeBuilder<WarehouseOrderPickingEvent> builder)
    {
        builder.ToTable("warehouse_order_picking_events");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OperationId).IsUnique();
        builder.HasIndex(x => new { x.WarehouseOrderId, x.OccurredAtUtc });
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.PackedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ActorKind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(120).IsRequired();
        builder.HasOne<WarehouseOrderItem>().WithMany().HasForeignKey(x => x.WarehouseOrderItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<WarehouseOrder>().WithMany().HasForeignKey(x => x.WarehouseOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
