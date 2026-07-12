using Microsoft.EntityFrameworkCore;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;
using SubiektMobile.Infrastructure.Persistence.Application.Entities;

namespace SubiektMobile.Infrastructure.Persistence.Application;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Administrator> Administrators => Set<Administrator>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderAssignee> OrderAssignees => Set<OrderAssignee>();
    public DbSet<OrderPickingEvent> OrderPickingEvents => Set<OrderPickingEvent>();
    public DbSet<Pallet> Pallets => Set<Pallet>();
    public DbSet<PalletItem> PalletItems => Set<PalletItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("app");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly,
            type => type.Namespace ==
                "SubiektMobile.Infrastructure.Persistence.Application.Configurations");
    }
}
