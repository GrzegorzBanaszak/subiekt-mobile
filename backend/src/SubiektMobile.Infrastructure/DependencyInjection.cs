using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubiektMobile.Infrastructure.Persistence;

namespace SubiektMobile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SubiektGt");

        Console.WriteLine(connectionString);
        services.AddDbContext<SubiektDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // services.AddScoped<ITowaryReadService, TowaryReadService>();

        return services;
    }
}