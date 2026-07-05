using SubiektMobile.Application.Identity;

namespace SubiektMobile.Api.Startup;

public sealed class BootstrapAdministratorHostedService : IHostedService
{
    private const string ConfigurationSection = "Identity:BootstrapAdministrator";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BootstrapAdministratorHostedService> _logger;

    public BootstrapAdministratorHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BootstrapAdministratorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var username = _configuration[$"{ConfigurationSection}:Username"];
        var displayName = _configuration[$"{ConfigurationSection}:DisplayName"];
        var password = _configuration[$"{ConfigurationSection}:Password"];

        if (AllMissing(username, displayName, password))
        {
            _logger.LogInformation("Automatic bootstrap administrator provisioning is not configured.");
            return;
        }

        var missingKeys = MissingConfigurationKeys(username, displayName, password);
        if (missingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Automatic bootstrap administrator provisioning is incomplete. Missing configuration: {string.Join(", ", missingKeys)}.");
        }

        using var scope = _scopeFactory.CreateScope();
        var identityAccessService = scope.ServiceProvider.GetRequiredService<IIdentityAccessService>();
        var result = await identityAccessService.EnsureBootstrapAdministratorAsync(
            new BootstrapAdministratorRequest(username!, displayName!, password!),
            cancellationToken);

        if (result == BootstrapAdministratorProvisioningResult.Created)
        {
            _logger.LogInformation("Bootstrap administrator was provisioned from startup configuration.");
        }
        else
        {
            _logger.LogInformation("Bootstrap administrator already exists; startup provisioning was skipped.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool AllMissing(params string?[] values) =>
        values.All(string.IsNullOrWhiteSpace);

    private static IReadOnlyList<string> MissingConfigurationKeys(
        string? username,
        string? displayName,
        string? password)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{ConfigurationSection}:Username"] = username,
            [$"{ConfigurationSection}:DisplayName"] = displayName,
            [$"{ConfigurationSection}:Password"] = password
        };

        return values
            .Where(entry => string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => entry.Key)
            .ToList();
    }
}
