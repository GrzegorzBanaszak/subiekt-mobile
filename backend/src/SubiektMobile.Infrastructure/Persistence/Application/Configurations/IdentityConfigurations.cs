using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Infrastructure.Persistence.Application.Entities;

namespace SubiektMobile.Infrastructure.Persistence.Application.Configurations;

public sealed class AdministratorConfiguration : IEntityTypeConfiguration<Administrator>
{
    public void Configure(EntityTypeBuilder<Administrator> builder)
    {
        builder.ToTable("administrators");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NormalizedUsername).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.RequiresPasswordChange).IsRequired();
        builder.HasIndex(x => x.NormalizedUsername).IsUnique();
        builder.HasIndex(x => x.IsBootstrapAdministrator)
            .IsUnique()
            .HasFilter("\"IsBootstrapAdministrator\" = TRUE");
    }
}

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NormalizedCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.NormalizedCode).IsUnique();
    }
}

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NormalizedCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.OrganizationId, x.NormalizedCode }).IsUnique();
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuthenticationSessionConfiguration : IEntityTypeConfiguration<AuthenticationSession>
{
    public void Configure(EntityTypeBuilder<AuthenticationSession> builder)
    {
        builder.ToTable("authentication_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorKind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.AdministratorId, x.RevokedAtUtc });
        builder.HasIndex(x => new { x.EmployeeId, x.RevokedAtUtc });
        builder.HasOne<Administrator>()
            .WithMany()
            .HasForeignKey(x => x.AdministratorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorKind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetType).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.ActorKind, x.ActorId });
        builder.HasIndex(x => new { x.TargetType, x.TargetId, x.Action, x.OccurredAtUtc });
    }
}
