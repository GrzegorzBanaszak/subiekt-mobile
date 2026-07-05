using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Infrastructure.Persistence.Application.Entities;

public sealed class AuthenticationSession
{
    private AuthenticationSession()
    {
    }

    public AuthenticationSession(
        Guid id,
        string tokenHash,
        ActorKind actorKind,
        Guid? administratorId,
        Guid? employeeId,
        Guid? organizationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        TokenHash = tokenHash;
        ActorKind = actorKind;
        AdministratorId = administratorId;
        EmployeeId = employeeId;
        OrganizationId = organizationId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public ActorKind ActorKind { get; private set; }
    public Guid? AdministratorId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAtUtc ??= now;
    }
}
