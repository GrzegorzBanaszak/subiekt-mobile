using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Domain.Customers;

namespace SubiektMobile.Infrastructure.Persistence.Application.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NormalizedCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.TaxId).HasMaxLength(32);
        builder.Property(x => x.InternalNotes).HasMaxLength(2000);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.NormalizedCode).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.UpdatedAtUtc });
        builder.HasMany(x => x.Sites).WithOne().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Sites).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class CustomerSiteConfiguration : IEntityTypeConfiguration<CustomerSite>
{
    public void Configure(EntityTypeBuilder<CustomerSite> builder)
    {
        builder.ToTable("customer_sites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NormalizedCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.CustomerId, x.NormalizedCode }).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.IsActive });
        builder.HasOne(x => x.LogisticsProfile).WithOne().HasForeignKey<CustomerLogisticsProfile>(x => x.CustomerSiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CustomerLogisticsProfileConfiguration : IEntityTypeConfiguration<CustomerLogisticsProfile>
{
    public void Configure(EntityTypeBuilder<CustomerLogisticsProfile> builder)
    {
        builder.ToTable("customer_logistics_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecipientName).HasMaxLength(120);
        builder.Property(x => x.Street).HasMaxLength(160);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.City).HasMaxLength(80);
        builder.Property(x => x.DefaultDock).HasMaxLength(120);
        builder.Property(x => x.ReceivingHours).HasMaxLength(160);
        builder.Property(x => x.SupplierNumber).HasMaxLength(64);
        builder.Property(x => x.DefaultPalletType).HasMaxLength(120);
        builder.Property(x => x.MaximumPalletHeightCm).HasPrecision(10, 2);
        builder.Property(x => x.LoadSecuringNotes).HasMaxLength(1000);
        builder.Property(x => x.LabelProfile).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.CustomerSiteId).IsUnique();
    }
}
