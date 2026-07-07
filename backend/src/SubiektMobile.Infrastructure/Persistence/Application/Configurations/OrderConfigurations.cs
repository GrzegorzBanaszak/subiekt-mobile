using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Domain.Orders;

namespace SubiektMobile.Infrastructure.Persistence.Application.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.UpdatedByName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.PickingMode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Assignees).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Assignees).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.Status, x.UpdatedAtUtc });
    }
}

public sealed class OrderAssigneeConfiguration : IEntityTypeConfiguration<OrderAssignee>
{
    public void Configure(EntityTypeBuilder<OrderAssignee> builder)
    {
        builder.ToTable("order_assignees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeDisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AssignedByName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.OrderId, x.EmployeeId }).IsUnique();
        builder.HasIndex(x => x.EmployeeId);
        builder.HasOne<SubiektMobile.Domain.Identity.Employee>().WithMany()
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SubiektMobile.Domain.Identity.Organization>().WithMany()
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProductSymbol).HasMaxLength(50);
        builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UnitWeightKg).HasPrecision(18, 4);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.ReservedByKind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ReservedByName).HasMaxLength(120);
        builder.Property(x => x.PackedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.PackedByKind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PackedByName).HasMaxLength(120);
        builder.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        builder.HasIndex(x => new { x.OrderId, x.Status });
    }
}

public sealed class OrderPickingEventConfiguration : IEntityTypeConfiguration<OrderPickingEvent>
{
    public void Configure(EntityTypeBuilder<OrderPickingEvent> builder)
    {
        builder.ToTable("order_picking_events");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OperationId).IsUnique();
        builder.HasIndex(x => new { x.OrderId, x.OccurredAtUtc });
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.PackedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ActorKind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(120).IsRequired();
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
