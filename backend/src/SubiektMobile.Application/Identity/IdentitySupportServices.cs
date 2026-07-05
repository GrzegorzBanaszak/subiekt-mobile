using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Application.Identity;

public sealed class ApplicationAuthorizationService : IApplicationAuthorizationService
{
    private readonly ICurrentActorAccessor _currentActorAccessor;

    public ApplicationAuthorizationService(ICurrentActorAccessor currentActorAccessor)
    {
        _currentActorAccessor = currentActorAccessor;
    }

    public CurrentActor RequireAuthenticated() =>
        _currentActorAccessor.Actor
        ?? throw new AccessDeniedException("Authentication is required.");

    public CurrentActor Require(string permission)
    {
        var actor = RequireAuthenticated();
        if (!actor.Permissions.Contains(permission, StringComparer.Ordinal))
        {
            throw new AccessDeniedException("The current actor does not have the required permission.");
        }

        return actor;
    }
}

public sealed class AuditEntryFactory : IAuditEntryFactory
{
    public AuditEntry Create(
        CurrentActor actor,
        string action,
        string targetType,
        Guid? targetId,
        DateTimeOffset occurredAtUtc) =>
        AuditEntry.Create(
            actor.Kind,
            actor.Id,
            actor.OrganizationId,
            actor.DisplayName,
            action,
            targetType,
            targetId,
            occurredAtUtc);
}
