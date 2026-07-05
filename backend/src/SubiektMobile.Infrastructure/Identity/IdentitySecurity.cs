using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SubiektMobile.Application.Identity;
using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Infrastructure.Identity;

public sealed class IdentityPasswordService : IPasswordService
{
    private readonly PasswordHasher<Administrator> _hasher = new();

    public string Hash(Administrator administrator, string password) =>
        _hasher.HashPassword(administrator, password);

    public bool Verify(Administrator administrator, string password) =>
        _hasher.VerifyHashedPassword(administrator, administrator.PasswordHash, password)
        is not PasswordVerificationResult.Failed;
}

public sealed class IdentityConfiguration : IIdentityConfiguration
{
    private readonly string _bootstrapToken;

    public IdentityConfiguration(IConfiguration configuration)
    {
        _bootstrapToken = configuration["Identity:BootstrapToken"] ?? string.Empty;
        AdministratorSessionLifetime = ReadLifetime(configuration, "Identity:AdministratorSessionHours", 8);
        EmployeeSessionLifetime = ReadLifetime(configuration, "Identity:EmployeeSessionHours", 12);
    }

    public TimeSpan AdministratorSessionLifetime { get; }
    public TimeSpan EmployeeSessionLifetime { get; }

    public bool IsValidBootstrapToken(string token)
    {
        if (_bootstrapToken.Length < 32 || string.IsNullOrEmpty(token))
        {
            return false;
        }

        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(_bootstrapToken));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static TimeSpan ReadLifetime(IConfiguration configuration, string key, int defaultHours)
    {
        var value = configuration.GetValue<int?>(key) ?? defaultHours;
        if (value is < 1 or > 168)
        {
            throw new InvalidOperationException($"Configuration value '{key}' must be between 1 and 168 hours.");
        }

        return TimeSpan.FromHours(value);
    }
}
