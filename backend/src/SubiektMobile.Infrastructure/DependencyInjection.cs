using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubiektMobile.Application.Products;
using SubiektMobile.Application.Identity;
using SubiektMobile.Infrastructure.Identity;
using SubiektMobile.Infrastructure.Persistence;
using SubiektMobile.Infrastructure.Persistence.Application;
using SubiektMobile.Infrastructure.Products;
using SubiektMobile.Application.Orders;
using SubiektMobile.Infrastructure.Orders;

namespace SubiektMobile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var subiektConnectionString = GetRequiredConnectionString(configuration, "SubiektGt");
        var applicationConnectionString = GetRequiredConnectionString(configuration, "ApplicationDb");

        services.AddDbContext<SubiektDbContext>(options =>
        {
            options.UseSqlServer(subiektConnectionString);
        });

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(applicationConnectionString);
        });

        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddScoped<IIdentityAccessStore, IdentityAccessStore>();
        services.AddScoped<IOrderStore, OrderStore>();
        services.AddSingleton<IPasswordService, IdentityPasswordService>();
        services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
        services.AddSingleton<IIdentityConfiguration, IdentityConfiguration>();

        return services;
    }

    private static string GetRequiredConnectionString(
        IConfiguration configuration,
        string name)
    {
        var connectionString = configuration.GetConnectionString(name);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"Missing required connection string 'ConnectionStrings:{name}'. " +
            "Configure it with user-secrets, environment variables, or local configuration outside the repository.");
    }
}
