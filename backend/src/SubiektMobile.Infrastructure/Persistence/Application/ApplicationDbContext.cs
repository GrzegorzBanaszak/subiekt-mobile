using Microsoft.EntityFrameworkCore;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;
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
    public DbSet<WarehouseOrder> WarehouseOrders => Set<WarehouseOrder>();
    public DbSet<WarehouseOrderItem> WarehouseOrderItems => Set<WarehouseOrderItem>();
    public DbSet<WarehouseOrderAssignee> WarehouseOrderAssignees => Set<WarehouseOrderAssignee>();
    public DbSet<WarehouseOrderPickingEvent> WarehouseOrderPickingEvents => Set<WarehouseOrderPickingEvent>();
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
