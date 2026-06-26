using Microsoft.EntityFrameworkCore;
using SubiektMobile.Infrastructure.Persistence.Entities;

namespace SubiektMobile.Infrastructure.Persistence;

public class SubiektDbContext : DbContext
{
    public SubiektDbContext(DbContextOptions<SubiektDbContext> options)
        : base(options)
    {
    }

    public DbSet<TwTowar> Towary => Set<TwTowar>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubiektDbContext).Assembly);
    }
}
