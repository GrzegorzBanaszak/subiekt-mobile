using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Application.Identity;

public static class Permissions
{
    public const string IdentityManage = "identity.manage";
    public const string CatalogRead = "catalog.read";
    public const string OrdersReadPublished = "orders.read-published";
    public const string OrdersManage = "orders.manage";
    public const string PickingExecute = "picking.execute";
    public const string PalletsManage = "pallets.manage";

    public static IReadOnlyList<string> For(ActorKind actorKind) => actorKind switch
    {
        ActorKind.Administrator =>
        [
            IdentityManage,
            CatalogRead,
            OrdersReadPublished,
            OrdersManage,
            PickingExecute,
            PalletsManage
        ],
        ActorKind.Employee =>
        [
            CatalogRead,
            OrdersReadPublished,
            PickingExecute,
            PalletsManage
        ],
        _ => []
    };
}

public sealed record CurrentActor(
    ActorKind Kind,
    Guid Id,
    Guid? OrganizationId,
    string DisplayName,
    IReadOnlyList<string> Permissions,
    Guid SessionId);

public sealed record SessionIssued(
    string Token,
    DateTimeOffset ExpiresAtUtc,
    CurrentActor Actor);

public sealed record AdministratorDto(
    Guid Id,
    string Username,
    string DisplayName,
    bool IsActive,
    bool IsBootstrapAdministrator,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record OrganizationDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EmployeeDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PublicOrganizationDto(Guid Id, string Code, string Name);
public sealed record PublicEmployeeDto(Guid Id, Guid OrganizationId, string Code, string DisplayName);

public sealed record BootstrapAdministratorRequest(string Username, string DisplayName, string Password);
public sealed record AdministratorSignInRequest(string Username, string Password);
public sealed record SelectEmployeeRequest(Guid OrganizationId, Guid EmployeeId);
public sealed record CreateAdministratorRequest(string Username, string DisplayName, string Password);
public sealed record UpdateAdministratorRequest(string Username, string DisplayName);
public sealed record ResetAdministratorPasswordRequest(string Password);
public sealed record SetActiveRequest(bool IsActive);
public sealed record CreateOrganizationRequest(string Code, string Name);
public sealed record UpdateOrganizationRequest(string Code, string Name);
public sealed record CreateEmployeeRequest(string Code, string DisplayName);
public sealed record UpdateEmployeeRequest(string Code, string DisplayName);

public interface ICurrentActorAccessor
{
    CurrentActor? Actor { get; }
}

public interface IApplicationAuthorizationService
{
    CurrentActor Require(string permission);
}

public interface IAuditEntryFactory
{
    AuditEntry Create(
        CurrentActor actor,
        string action,
        string targetType,
        Guid? targetId,
        DateTimeOffset occurredAtUtc);
}

public interface IPasswordService
{
    string Hash(Administrator administrator, string password);
    bool Verify(Administrator administrator, string password);
}

public interface IIdentityConfiguration
{
    TimeSpan AdministratorSessionLifetime { get; }
    TimeSpan EmployeeSessionLifetime { get; }
    bool IsValidBootstrapToken(string token);
}

public enum StoreMutationResult
{
    Success,
    NotFound,
    Conflict,
    LastActiveAdministrator
}

public enum BootstrapAdministratorProvisioningResult
{
    Created,
    AlreadyExists
}

public interface IIdentityAccessStore
{
    Task<bool> BootstrapAdministratorExistsAsync(CancellationToken cancellationToken);
    Task<Administrator?> FindAdministratorByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);
    Task<Administrator?> FindAdministratorAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Administrator>> ListAdministratorsAsync(CancellationToken cancellationToken);
    Task<StoreMutationResult> CreateAdministratorAsync(
        Administrator administrator,
        AuditEntry auditEntry,
        CancellationToken cancellationToken);
    Task<StoreMutationResult> UpdateAdministratorAsync(
        Administrator administrator,
        AuditEntry auditEntry,
        CancellationToken cancellationToken);
    Task<StoreMutationResult> ResetAdministratorPasswordAsync(
        Administrator administrator,
        AuditEntry auditEntry,
        CancellationToken cancellationToken);
    Task<StoreMutationResult> SetAdministratorActiveAsync(
        Guid administratorId,
        bool isActive,
        AuditEntry auditEntry,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Organization>> ListOrganizationsAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<Organization?> FindOrganizationAsync(Guid id, CancellationToken cancellationToken);
    Task<StoreMutationResult> CreateOrganizationAsync(
        Organization organization,
        AuditEntry auditEntry,
        CancellationToken cancellationToken);
    Task<StoreMutationResult> UpdateOrganizationAsync(
        Organization organization,
        AuditEntry auditEntry,
        CancellationToken cancellationToken);
    Task<StoreMutationResult> SetOrganizationActiveAsync(
        Guid organizationId,
        bool isActive,
        AuditEntry auditEntry,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Employee>> ListEmployeesAsync(
        Guid organizationId,
        bool activeOnly,
        CancellationToken cancellationToken);
    Task<Employee?> FindEmployeeAsync(Guid organizationId, Guid employeeId, CancellationToken cancellationToken);
    Task<StoreMutationResult> CreateEmployeeAsync(
        Employee employee,
        AuditEntry auditEntry,
        CancellationToken cancellationToken);
    Task<StoreMutationResult> UpdateEmployeeAsync(
        Employee employee,
        AuditEntry auditEntry,
        CancellationToken cancellationToken);
    Task<StoreMutationResult> SetEmployeeActiveAsync(
        Guid organizationId,
        Guid employeeId,
        bool isActive,
        AuditEntry auditEntry,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<SessionIssued> CreateSessionAsync(
        ActorKind actorKind,
        Guid actorId,
        Guid? organizationId,
        string actorDisplayName,
        TimeSpan lifetime,
        string? replacedToken,
        AuditEntry auditEntry,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<CurrentActor?> ResolveSessionAsync(string token, DateTimeOffset now, CancellationToken cancellationToken);
    Task RevokeSessionAsync(string token, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IIdentityAccessService
{
    Task<BootstrapAdministratorProvisioningResult> EnsureBootstrapAdministratorAsync(
        BootstrapAdministratorRequest request,
        CancellationToken cancellationToken);
    Task<SessionIssued> BootstrapAdministratorAsync(
        BootstrapAdministratorRequest request,
        string setupToken,
        string? replacedSessionToken,
        CancellationToken cancellationToken);
    Task<SessionIssued> SignInAdministratorAsync(
        AdministratorSignInRequest request,
        string? replacedSessionToken,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicOrganizationDto>> ListPublicOrganizationsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicEmployeeDto>> ListPublicEmployeesAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<SessionIssued> SelectEmployeeAsync(
        SelectEmployeeRequest request,
        string? replacedSessionToken,
        CancellationToken cancellationToken);
    Task<CurrentActor?> ResolveSessionAsync(string token, CancellationToken cancellationToken);
    Task SignOutAsync(string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdministratorDto>> ListAdministratorsAsync(CancellationToken cancellationToken);
    Task<AdministratorDto> CreateAdministratorAsync(CreateAdministratorRequest request, CancellationToken cancellationToken);
    Task<AdministratorDto> UpdateAdministratorAsync(Guid id, UpdateAdministratorRequest request, CancellationToken cancellationToken);
    Task ResetAdministratorPasswordAsync(Guid id, ResetAdministratorPasswordRequest request, CancellationToken cancellationToken);
    Task SetAdministratorActiveAsync(Guid id, SetActiveRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrganizationDto>> ListOrganizationsAsync(CancellationToken cancellationToken);
    Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken cancellationToken);
    Task<OrganizationDto> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken);
    Task SetOrganizationActiveAsync(Guid id, SetActiveRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeDto>> ListEmployeesAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<EmployeeDto> CreateEmployeeAsync(Guid organizationId, CreateEmployeeRequest request, CancellationToken cancellationToken);
    Task<EmployeeDto> UpdateEmployeeAsync(
        Guid organizationId,
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken);
    Task SetEmployeeActiveAsync(
        Guid organizationId,
        Guid employeeId,
        SetActiveRequest request,
        CancellationToken cancellationToken);
}

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string message) : base(message) { }
}

public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}

public sealed class ResourceConflictException : Exception
{
    public ResourceConflictException(string message) : base(message) { }
}

public sealed class AccessDeniedException : Exception
{
    public AccessDeniedException(string message) : base(message) { }
}

public sealed class SignInFailedException : Exception
{
    public SignInFailedException() : base("Invalid credentials or inactive account.") { }
}
