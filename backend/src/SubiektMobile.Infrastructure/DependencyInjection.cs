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

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing required connection string 'ConnectionStrings:SubiektGt'. Configure it with user-secrets, environment variables, or local configuration outside the repository.");
        }

        services.AddDbContext<SubiektDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}
