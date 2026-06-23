using Microsoft.EntityFrameworkCore;

namespace SubiektMobile.Infrastructure.Persistence;

public class SubiektDbContext : DbContext
{
    public SubiektDbContext(DbContextOptions<SubiektDbContext> options)
        : base(options)
    {
    }


}