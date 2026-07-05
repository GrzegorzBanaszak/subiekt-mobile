using System.Text.RegularExpressions;

namespace SubiektMobile.Domain.Identity;

public enum ActorKind
{
    System,
    Administrator,
    Employee
}

public sealed class Administrator
{
    private Administrator()
    {
    }

    private Administrator(
        Guid id,
        string username,
        string displayName,
        string passwordHash,
        bool isBootstrapAdministrator,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SetUsername(username);
        SetDisplayName(displayName);
        SetPasswordHash(passwordHash);
        IsBootstrapAdministrator = isBootstrapAdministrator;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsBootstrapAdministrator { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Administrator Create(
        string username,
        string displayName,
        string passwordHash,
        bool isBootstrapAdministrator,
        DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), username, displayName, passwordHash, isBootstrapAdministrator, createdAtUtc);

    public void Update(string username, string displayName, DateTimeOffset now)
    {
        SetUsername(username);
        SetDisplayName(displayName);
        UpdatedAtUtc = now;
    }

    public void SetPasswordHash(string passwordHash, DateTimeOffset now)
    {
        SetPasswordHash(passwordHash);
        UpdatedAtUtc = now;
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    private void SetUsername(string username)
    {
        Username = IdentityRules.RequireLogin(username);
        NormalizedUsername = IdentityRules.Normalize(Username);
    }

    private void SetDisplayName(string displayName) =>
        DisplayName = IdentityRules.RequireDisplayName(displayName);

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }
}

public sealed class Organization
{
    private Organization()
    {
    }

    private Organization(Guid id, string code, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        SetCode(code);
        SetName(name);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Organization Create(string code, string name, DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), code, name, createdAtUtc);

    public void Update(string code, string name, DateTimeOffset now)
    {
        SetCode(code);
        SetName(name);
        UpdatedAtUtc = now;
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    private void SetCode(string code)
    {
        Code = IdentityRules.RequireCode(code, nameof(code));
        NormalizedCode = IdentityRules.Normalize(Code);
    }

    private void SetName(string name) => Name = IdentityRules.RequireDisplayName(name);
}

public sealed class Employee
{
    private Employee()
    {
    }

    private Employee(
        Guid id,
        Guid organizationId,
        string code,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        SetCode(code);
        SetDisplayName(displayName);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Employee Create(
        Guid organizationId,
        string code,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization is required.", nameof(organizationId));
        }

        return new Employee(Guid.NewGuid(), organizationId, code, displayName, createdAtUtc);
    }

    public void Update(string code, string displayName, DateTimeOffset now)
    {
        SetCode(code);
        SetDisplayName(displayName);
        UpdatedAtUtc = now;
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    private void SetCode(string code)
    {
        Code = IdentityRules.RequireCode(code, nameof(code));
        NormalizedCode = IdentityRules.Normalize(Code);
    }

    private void SetDisplayName(string displayName) =>
        DisplayName = IdentityRules.RequireDisplayName(displayName);
}

public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    private AuditEntry(
        Guid id,
        ActorKind actorKind,
        Guid? actorId,
        Guid? organizationId,
        string actorDisplayName,
        string action,
        string targetType,
        Guid? targetId,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        ActorKind = actorKind;
        ActorId = actorId;
        OrganizationId = organizationId;
        ActorDisplayName = actorDisplayName;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public ActorKind ActorKind { get; private set; }
    public Guid? ActorId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string ActorDisplayName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty;
    public Guid? TargetId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditEntry Create(
        ActorKind actorKind,
        Guid? actorId,
        Guid? organizationId,
        string actorDisplayName,
        string action,
        string targetType,
        Guid? targetId,
        DateTimeOffset occurredAtUtc) =>
        new(
            Guid.NewGuid(),
            actorKind,
            actorId,
            organizationId,
            IdentityRules.RequireAuditText(actorDisplayName, nameof(actorDisplayName), 120),
            IdentityRules.RequireAuditText(action, nameof(action), 100),
            IdentityRules.RequireAuditText(targetType, nameof(targetType), 80),
            targetId,
            occurredAtUtc);
}

public static partial class IdentityRules
{
    public const int MinimumPasswordLength = 12;

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public static string RequireLogin(string value)
    {
        var result = RequireLength(value, nameof(value), 3, 64);
        if (!LoginPattern().IsMatch(result))
        {
            throw new ArgumentException(
                "Login may contain letters, digits and the characters '.', '_', '@', '-'.",
                nameof(value));
        }

        return result;
    }

    public static string RequireCode(string value, string parameterName)
    {
        var result = RequireLength(value, parameterName, 2, 32);
        if (!CodePattern().IsMatch(result))
        {
            throw new ArgumentException(
                "Code may contain letters, digits and the characters '_', '-'.",
                parameterName);
        }

        return result;
    }

    public static string RequireDisplayName(string value) =>
        RequireLength(value, nameof(value), 2, 120);

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength || password.Length > 128)
        {
            throw new ArgumentException(
                $"Password must contain between {MinimumPasswordLength} and 128 characters.",
                nameof(password));
        }
    }

    public static string RequireAuditText(string value, string parameterName, int maximumLength) =>
        RequireLength(value, parameterName, 1, maximumLength);

    private static string RequireLength(string value, string parameterName, int minimum, int maximum)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length < minimum || result.Length > maximum)
        {
            throw new ArgumentException(
                $"Value must contain between {minimum} and {maximum} characters.",
                parameterName);
        }

        return result;
    }

    [GeneratedRegex("^[\\p{L}\\p{N}._@-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex LoginPattern();

    [GeneratedRegex("^[\\p{L}\\p{N}_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
