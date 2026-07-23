using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Domain.CustomerOrders;
using SubiektMobile.Domain.Customers;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Infrastructure.Persistence.Application.Configurations;

public sealed class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
{
    public void Configure(EntityTypeBuilder<CustomerOrder> builder)
    {
        builder.ToTable("customer_orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerOrderNumber).HasMaxLength(80);
        builder.Property(x => x.DeliveryNoteNumber).HasMaxLength(80);
        builder.Property(x => x.CustomerNotes).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.UpdatedByName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.Status, x.UpdatedAtUtc });
        builder.HasIndex(x => new { x.CustomerId, x.RequestedDeliveryDate });
        builder.HasIndex(x => new { x.CustomerSiteId, x.RequestedDeliveryDate });
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustomerSite>().WithMany().HasForeignKey(x => x.CustomerSiteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class CustomerOrderItemConfiguration : IEntityTypeConfiguration<CustomerOrderItem>
{
    public void Configure(EntityTypeBuilder<CustomerOrderItem> builder)
    {
        builder.ToTable("customer_order_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerPartNumber).HasMaxLength(80).IsRequired();
        builder.Property(x => x.NormalizedCustomerPartNumber).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
        builder.HasIndex(x => new { x.CustomerOrderId, x.NormalizedCustomerPartNumber });
    }
}

public sealed class CustomerOrderWarehouseOrderConfiguration : IEntityTypeConfiguration<WarehouseOrder>
{
    public void Configure(EntityTypeBuilder<WarehouseOrder> builder)
    {
        builder.Property(x => x.CustomerDeliveryNoteNumber).HasMaxLength(80);
        builder.HasIndex(x => x.CustomerOrderId).IsUnique().HasFilter("\"CustomerOrderId\" IS NOT NULL");
        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CustomerOrderWarehouseOrderItemConfiguration : IEntityTypeConfiguration<WarehouseOrderItem>
{
    public void Configure(EntityTypeBuilder<WarehouseOrderItem> builder)
    {
        builder.Property(x => x.CustomerPartNumber).HasMaxLength(80);
        builder.Property(x => x.EngineeringChange).HasMaxLength(80);
        builder.Property(x => x.CustomerPackagingCode).HasMaxLength(64);
        builder.HasIndex(x => x.CustomerOrderItemId).IsUnique().HasFilter("\"CustomerOrderItemId\" IS NOT NULL");
        builder.HasOne<CustomerOrderItem>().WithMany().HasForeignKey(x => x.CustomerOrderItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
