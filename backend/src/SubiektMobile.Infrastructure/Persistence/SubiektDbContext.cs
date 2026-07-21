using Microsoft.EntityFrameworkCore;
using SubiektMobile.Infrastructure.Persistence.Configurations;
using SubiektMobile.Infrastructure.Persistence.Entities;

namespace SubiektMobile.Infrastructure.Persistence;

public class SubiektDbContext : DbContext
{
    public SubiektDbContext(DbContextOptions<SubiektDbContext> options)
        : base(options)
    {
    }

    public DbSet<TwTowar> Towary => Set<TwTowar>();
    public DbSet<TwCena> Ceny => Set<TwCena>();
    public DbSet<TwParametr> ParametryTowarow => Set<TwParametr>();
    public DbSet<TwStan> StanyTowarow => Set<TwStan>();
    public DbSet<SlMagazyn> Magazyny => Set<SlMagazyn>();
    public DbSet<TwKodKreskowy> KodyKreskowe => Set<TwKodKreskowy>();
    public DbSet<TwZdjecieTw> ZdjeciaTowarow => Set<TwZdjecieTw>();
    public DbSet<SlStawkaVat> StawkiVat => Set<SlStawkaVat>();
    public DbSet<KhKontrahent> Kontrahenci => Set<KhKontrahent>();
    public DbSet<AdrEwid> Adresy => Set<AdrEwid>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SubiektDbContext).Assembly,
            type => type.Namespace == typeof(TwTowarConfiguration).Namespace);
    }
}
