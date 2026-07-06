using Microsoft.Extensions.DependencyInjection;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Orders;

namespace SubiektMobile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddScoped<IIdentityAccessService, IdentityAccessService>();
        services.AddScoped<IApplicationAuthorizationService, ApplicationAuthorizationService>();
        services.AddSingleton<IAuditEntryFactory, AuditEntryFactory>();
        services.AddSingleton<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
