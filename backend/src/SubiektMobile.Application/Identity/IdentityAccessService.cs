using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Application.Identity;

public sealed class IdentityAccessService : IIdentityAccessService
{
    private readonly IIdentityAccessStore _store;
    private readonly IPasswordService _passwordService;
    private readonly ITemporaryPasswordGenerator _temporaryPasswordGenerator;
    private readonly IIdentityConfiguration _configuration;
    private readonly IApplicationAuthorizationService _authorizationService;
    private readonly IAuditEntryFactory _auditEntryFactory;
    private readonly TimeProvider _timeProvider;

    public IdentityAccessService(
        IIdentityAccessStore store,
        IPasswordService passwordService,
        ITemporaryPasswordGenerator temporaryPasswordGenerator,
        IIdentityConfiguration configuration,
        IApplicationAuthorizationService authorizationService,
        IAuditEntryFactory auditEntryFactory,
        TimeProvider timeProvider)
    {
        _store = store;
        _passwordService = passwordService;
        _temporaryPasswordGenerator = temporaryPasswordGenerator;
        _configuration = configuration;
        _authorizationService = authorizationService;
        _auditEntryFactory = auditEntryFactory;
        _timeProvider = timeProvider;
    }

    public async Task<BootstrapAdministratorProvisioningResult> EnsureBootstrapAdministratorAsync(
        BootstrapAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        if (await _store.BootstrapAdministratorExistsAsync(cancellationToken))
        {
            return BootstrapAdministratorProvisioningResult.AlreadyExists;
        }

        var now = UtcNow();
        var administrator = CreateBootstrapAdministrator(request, now);
        var result = await _store.CreateAdministratorAsync(
            administrator,
            SystemAudit("AdministratorBootstrapped", "Administrator", administrator.Id, now),
            cancellationToken);

        if (result == StoreMutationResult.Success)
        {
            return BootstrapAdministratorProvisioningResult.Created;
        }

        if (result == StoreMutationResult.Conflict
            && await _store.BootstrapAdministratorExistsAsync(cancellationToken))
        {
            return BootstrapAdministratorProvisioningResult.AlreadyExists;
        }

        EnsureMutation(result, "Administrator");
        throw new InvalidOperationException("Unreachable provisioning result.");
    }

    public async Task<SessionIssued> BootstrapAdministratorAsync(
        BootstrapAdministratorRequest request,
        string setupToken,
        string? replacedSessionToken,
        CancellationToken cancellationToken)
    {
        if (!_configuration.IsValidBootstrapToken(setupToken))
        {
            throw new AccessDeniedException("Invalid setup token.");
        }

        var now = UtcNow();
        var administrator = CreateBootstrapAdministrator(request, now);

        var audit = SystemAudit("AdministratorBootstrapped", "Administrator", administrator.Id, now);
        var result = await _store.CreateAdministratorAsync(administrator, audit, cancellationToken);
        EnsureMutation(result, "Administrator", bootstrap: true);

        return await IssueAdministratorSessionAsync(
            administrator,
            replacedSessionToken,
            "AdministratorSignedIn",
            now,
            cancellationToken);
    }

    public async Task<SessionIssued> SignInAdministratorAsync(
        AdministratorSignInRequest request,
        string? replacedSessionToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            throw new SignInFailedException();
        }

        var normalizedUsername = IdentityRules.Normalize(request.Username ?? string.Empty);
        var administrator = await _store.FindAdministratorByUsernameAsync(normalizedUsername, cancellationToken);

        if (administrator is null || !administrator.IsActive || !_passwordService.Verify(administrator, request.Password))
        {
            throw new SignInFailedException();
        }

        return await IssueAdministratorSessionAsync(
            administrator,
            replacedSessionToken,
            "AdministratorSignedIn",
            UtcNow(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<PublicOrganizationDto>> ListPublicOrganizationsAsync(CancellationToken cancellationToken) =>
        (await _store.ListOrganizationsAsync(activeOnly: true, cancellationToken))
            .Select(value => new PublicOrganizationDto(value.Id, value.Code, value.Name))
            .ToList();

    public async Task<IReadOnlyList<PublicEmployeeDto>> ListPublicEmployeesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var organization = await _store.FindOrganizationAsync(organizationId, cancellationToken);
        if (organization is null || !organization.IsActive)
        {
            throw new ResourceNotFoundException("Organization was not found.");
        }

        return (await _store.ListEmployeesAsync(organizationId, activeOnly: true, cancellationToken))
            .Select(value => new PublicEmployeeDto(value.Id, value.OrganizationId, value.Code, value.DisplayName))
            .ToList();
    }

    public async Task<SessionIssued> SelectEmployeeAsync(
        SelectEmployeeRequest request,
        string? replacedSessionToken,
        CancellationToken cancellationToken)
    {
        var organization = await _store.FindOrganizationAsync(request.OrganizationId, cancellationToken);
        var employee = await _store.FindEmployeeAsync(request.OrganizationId, request.EmployeeId, cancellationToken);

        if (organization is null || employee is null || !organization.IsActive || !employee.IsActive)
        {
            throw new ResourceNotFoundException("Active organization and employee were not found.");
        }

        var now = UtcNow();
        var audit = ActorAudit(
            ActorKind.Employee,
            employee.Id,
            organization.Id,
            employee.DisplayName,
            "EmployeeSelected",
            "Employee",
            employee.Id,
            now);

        return await _store.CreateSessionAsync(
            ActorKind.Employee,
            employee.Id,
            organization.Id,
            employee.DisplayName,
            Permissions.For(ActorKind.Employee),
            _configuration.EmployeeSessionLifetime,
            replacedSessionToken,
            audit,
            now,
            cancellationToken);
    }

    public Task<CurrentActor?> ResolveSessionAsync(string token, CancellationToken cancellationToken) =>
        _store.ResolveSessionAsync(token, UtcNow(), cancellationToken);

    public Task SignOutAsync(string token, CancellationToken cancellationToken) =>
        _store.RevokeSessionAsync(token, UtcNow(), cancellationToken);

    public async Task ChangeOwnPasswordAsync(
        ChangeOwnPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var actor = _authorizationService.RequireAuthenticated();
        if (actor.Kind != ActorKind.Administrator)
        {
            throw new AccessDeniedException("Only an administrator can change an administrator password.");
        }

        ValidatePassword(request.NewPassword);
        var administrator = await GetAdministratorAsync(actor.Id, cancellationToken);
        if (!_passwordService.Verify(administrator, request.CurrentPassword))
        {
            throw new SignInFailedException();
        }

        if (_passwordService.Verify(administrator, request.NewPassword))
        {
            throw new RequestValidationException("The new password must be different from the current password.");
        }

        var now = UtcNow();
        administrator.ChangePassword(_passwordService.Hash(administrator, request.NewPassword), now);
        var result = await _store.ChangeAdministratorPasswordAsync(
            administrator,
            actor.SessionId,
            Audit(actor, "AdministratorPasswordChanged", "Administrator", administrator.Id, now),
            cancellationToken);
        EnsureMutation(result, "Administrator");
    }

    public async Task<IReadOnlyList<AdministratorDto>> ListAdministratorsAsync(CancellationToken cancellationToken)
    {
        Require(Permissions.AdministratorsManage);
        return (await _store.ListAdministratorsAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<CreatedAdministratorDto> CreateAdministratorAsync(
        CreateAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.AdministratorsManage);
        var temporaryPassword = _temporaryPasswordGenerator.Generate();
        ValidatePassword(temporaryPassword);
        var now = UtcNow();
        Administrator administrator;
        try
        {
            administrator = Administrator.Create(request.Username, request.DisplayName, "pending", false, now);
            administrator.SetPasswordHash(_passwordService.Hash(administrator, temporaryPassword), now);
            administrator.RequirePasswordChange(now);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception);
        }

        var result = await _store.CreateAdministratorAsync(
            administrator,
            Audit(actor, "AdministratorCreated", "Administrator", administrator.Id, now),
            cancellationToken);
        EnsureMutation(result, "Administrator");
        return new CreatedAdministratorDto(Map(administrator), temporaryPassword);
    }

    public async Task<AdministratorDto> UpdateAdministratorAsync(
        Guid id,
        UpdateAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.AdministratorsManage);
        var administrator = await GetAdministratorAsync(id, cancellationToken);
        var now = UtcNow();
        try
        {
            administrator.Update(request.Username, request.DisplayName, now);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception);
        }

        var result = await _store.UpdateAdministratorAsync(
            administrator,
            Audit(actor, "AdministratorUpdated", "Administrator", administrator.Id, now),
            cancellationToken);
        EnsureMutation(result, "Administrator");
        return Map(administrator);
    }

    public async Task ResetAdministratorPasswordAsync(
        Guid id,
        ResetAdministratorPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.AdministratorsManage);
        ValidatePassword(request.Password);
        var administrator = await GetAdministratorAsync(id, cancellationToken);
        var now = UtcNow();
        administrator.SetPasswordHash(_passwordService.Hash(administrator, request.Password), now);
        administrator.RequirePasswordChange(now);
        var result = await _store.ResetAdministratorPasswordAsync(
            administrator,
            Audit(actor, "AdministratorPasswordReset", "Administrator", administrator.Id, now),
            cancellationToken);
        EnsureMutation(result, "Administrator");
    }

    public async Task SetAdministratorActiveAsync(
        Guid id,
        SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.AdministratorsManage);
        if (actor.Id == id && !request.IsActive)
        {
            throw new ResourceConflictException("An administrator cannot deactivate the current account.");
        }

        var now = UtcNow();
        var result = await _store.SetAdministratorActiveAsync(
            id,
            request.IsActive,
            Audit(actor, request.IsActive ? "AdministratorActivated" : "AdministratorDeactivated", "Administrator", id, now),
            now,
            cancellationToken);
        EnsureMutation(result, "Administrator");
    }

    public async Task<IReadOnlyList<OrganizationDto>> ListOrganizationsAsync(CancellationToken cancellationToken)
    {
        Require(Permissions.IdentityManage);
        return (await _store.ListOrganizationsAsync(activeOnly: false, cancellationToken)).Select(Map).ToList();
    }

    public async Task<OrganizationDto> CreateOrganizationAsync(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.IdentityManage);
        var now = UtcNow();
        Organization organization;
        try
        {
            organization = Organization.Create(request.Code, request.Name, now);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception);
        }

        var result = await _store.CreateOrganizationAsync(
            organization,
            Audit(actor, "OrganizationCreated", "Organization", organization.Id, now),
            cancellationToken);
        EnsureMutation(result, "Organization");
        return Map(organization);
    }

    public async Task<OrganizationDto> UpdateOrganizationAsync(
        Guid id,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.IdentityManage);
        var organization = await GetOrganizationAsync(id, cancellationToken);
        var now = UtcNow();
        try
        {
            organization.Update(request.Code, request.Name, now);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception);
        }

        var result = await _store.UpdateOrganizationAsync(
            organization,
            Audit(actor, "OrganizationUpdated", "Organization", organization.Id, now),
            cancellationToken);
        EnsureMutation(result, "Organization");
        return Map(organization);
    }

    public async Task SetOrganizationActiveAsync(
        Guid id,
        SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.IdentityManage);
        var now = UtcNow();
        var result = await _store.SetOrganizationActiveAsync(
            id,
            request.IsActive,
            Audit(actor, request.IsActive ? "OrganizationActivated" : "OrganizationDeactivated", "Organization", id, now),
            now,
            cancellationToken);
        EnsureMutation(result, "Organization");
    }

    public async Task<IReadOnlyList<EmployeeDto>> ListEmployeesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        Require(Permissions.IdentityManage);
        await GetOrganizationAsync(organizationId, cancellationToken);
        return (await _store.ListEmployeesAsync(organizationId, activeOnly: false, cancellationToken)).Select(Map).ToList();
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(
        Guid organizationId,
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.IdentityManage);
        await GetOrganizationAsync(organizationId, cancellationToken);
        var now = UtcNow();
        Employee employee;
        try
        {
            employee = Employee.Create(organizationId, request.Code, request.DisplayName, now);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception);
        }

        var result = await _store.CreateEmployeeAsync(
            employee,
            Audit(actor, "EmployeeCreated", "Employee", employee.Id, now),
            cancellationToken);
        EnsureMutation(result, "Employee");
        return Map(employee);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(
        Guid organizationId,
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.IdentityManage);
        var employee = await GetEmployeeAsync(organizationId, employeeId, cancellationToken);
        var now = UtcNow();
        try
        {
            employee.Update(request.Code, request.DisplayName, now);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception);
        }

        var result = await _store.UpdateEmployeeAsync(
            employee,
            Audit(actor, "EmployeeUpdated", "Employee", employee.Id, now),
            cancellationToken);
        EnsureMutation(result, "Employee");
        return Map(employee);
    }

    public async Task SetEmployeeActiveAsync(
        Guid organizationId,
        Guid employeeId,
        SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var actor = Require(Permissions.IdentityManage);
        var now = UtcNow();
        var result = await _store.SetEmployeeActiveAsync(
            organizationId,
            employeeId,
            request.IsActive,
            Audit(actor, request.IsActive ? "EmployeeActivated" : "EmployeeDeactivated", "Employee", employeeId, now),
            now,
            cancellationToken);
        EnsureMutation(result, "Employee");
    }

    private async Task<SessionIssued> IssueAdministratorSessionAsync(
        Administrator administrator,
        string? replacedSessionToken,
        string action,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var audit = ActorAudit(
            ActorKind.Administrator,
            administrator.Id,
            null,
            administrator.DisplayName,
            action,
            "Administrator",
            administrator.Id,
            now);
        return await _store.CreateSessionAsync(
            ActorKind.Administrator,
            administrator.Id,
            null,
            administrator.DisplayName,
            administrator.RequiresPasswordChange
                ? []
                : Permissions.For(ActorKind.Administrator, administrator.IsBootstrapAdministrator),
            _configuration.AdministratorSessionLifetime,
            replacedSessionToken,
            audit,
            now,
            cancellationToken);
    }

    private CurrentActor Require(string permission)
        => _authorizationService.Require(permission);

    private async Task<Administrator> GetAdministratorAsync(Guid id, CancellationToken cancellationToken) =>
        await _store.FindAdministratorAsync(id, cancellationToken)
        ?? throw new ResourceNotFoundException("Administrator was not found.");

    private async Task<Organization> GetOrganizationAsync(Guid id, CancellationToken cancellationToken) =>
        await _store.FindOrganizationAsync(id, cancellationToken)
        ?? throw new ResourceNotFoundException("Organization was not found.");

    private async Task<Employee> GetEmployeeAsync(Guid organizationId, Guid employeeId, CancellationToken cancellationToken) =>
        await _store.FindEmployeeAsync(organizationId, employeeId, cancellationToken)
        ?? throw new ResourceNotFoundException("Employee was not found.");

    private static void EnsureMutation(StoreMutationResult result, string resource, bool bootstrap = false)
    {
        switch (result)
        {
            case StoreMutationResult.Success:
                return;
            case StoreMutationResult.NotFound:
                throw new ResourceNotFoundException($"{resource} was not found.");
            case StoreMutationResult.LastActiveAdministrator:
                throw new ResourceConflictException("The last active administrator cannot be deactivated.");
            case StoreMutationResult.Conflict when bootstrap:
                throw new ResourceConflictException("The application has already been bootstrapped.");
            default:
                throw new ResourceConflictException($"{resource} conflicts with an existing resource.");
        }
    }

    private static void ValidatePassword(string password)
    {
        try
        {
            IdentityRules.ValidatePassword(password);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception);
        }
    }

    private Administrator CreateBootstrapAdministrator(
        BootstrapAdministratorRequest request,
        DateTimeOffset? timestamp = null)
    {
        ValidatePassword(request.Password);
        var now = timestamp ?? UtcNow();

        try
        {
            var administrator = Administrator.Create(
                request.Username,
                request.DisplayName,
                "pending",
                isBootstrapAdministrator: true,
                now);
            administrator.SetPasswordHash(_passwordService.Hash(administrator, request.Password), now);
            return administrator;
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception);
        }
    }

    private static RequestValidationException Validation(ArgumentException exception) =>
        new(exception.Message);

    private DateTimeOffset UtcNow() => _timeProvider.GetUtcNow();

    private AuditEntry Audit(
        CurrentActor actor,
        string action,
        string targetType,
        Guid? targetId,
        DateTimeOffset now) =>
        _auditEntryFactory.Create(actor, action, targetType, targetId, now);

    private static AuditEntry ActorAudit(
        ActorKind kind,
        Guid actorId,
        Guid? organizationId,
        string actorDisplayName,
        string action,
        string targetType,
        Guid? targetId,
        DateTimeOffset now) =>
        AuditEntry.Create(kind, actorId, organizationId, actorDisplayName, action, targetType, targetId, now);

    private static AuditEntry SystemAudit(string action, string targetType, Guid? targetId, DateTimeOffset now) =>
        AuditEntry.Create(ActorKind.System, null, null, "System", action, targetType, targetId, now);

    private static AdministratorDto Map(Administrator value) =>
        new(value.Id, value.Username, value.DisplayName, value.IsActive, value.IsBootstrapAdministrator, value.RequiresPasswordChange, value.CreatedAtUtc, value.UpdatedAtUtc);

    private static OrganizationDto Map(Organization value) =>
        new(value.Id, value.Code, value.Name, value.IsActive, value.CreatedAtUtc, value.UpdatedAtUtc);

    private static EmployeeDto Map(Employee value) =>
        new(value.Id, value.OrganizationId, value.Code, value.DisplayName, value.IsActive, value.CreatedAtUtc, value.UpdatedAtUtc);
}
