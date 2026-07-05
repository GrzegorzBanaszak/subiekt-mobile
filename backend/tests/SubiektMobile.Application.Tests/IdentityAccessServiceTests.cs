using SubiektMobile.Application.Identity;
using SubiektMobile.Domain.Identity;
using Xunit;

namespace SubiektMobile.Application.Tests;

public sealed class IdentityAccessServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Employee_permissions_are_limited_and_equal_for_every_employee()
    {
        var permissions = Permissions.For(ActorKind.Employee);

        Assert.Contains(Permissions.CatalogRead, permissions);
        Assert.Contains(Permissions.PickingExecute, permissions);
        Assert.Contains(Permissions.PalletsManage, permissions);
        Assert.DoesNotContain(Permissions.IdentityManage, permissions);
        Assert.DoesNotContain(Permissions.AdministratorsManage, permissions);
        Assert.DoesNotContain(Permissions.OrdersManage, permissions);
    }

    [Fact]
    public void Only_bootstrap_administrator_can_manage_administrators()
    {
        var regularPermissions = Permissions.For(ActorKind.Administrator);
        var bootstrapPermissions = Permissions.For(ActorKind.Administrator, isBootstrapAdministrator: true);

        Assert.Contains(Permissions.IdentityManage, regularPermissions);
        Assert.DoesNotContain(Permissions.AdministratorsManage, regularPermissions);
        Assert.Contains(Permissions.IdentityManage, bootstrapPermissions);
        Assert.Contains(Permissions.AdministratorsManage, bootstrapPermissions);
    }

    [Fact]
    public async Task Selecting_employee_creates_audited_session_and_replaces_previous_token()
    {
        var organization = Organization.Create("MAG", "Magazyn", Now);
        var employee = Employee.Create(organization.Id, "P01", "Pracownik 01", Now);
        var store = new SelectingEmployeeStore(organization, employee);
        var service = CreateService(store, actor: null);

        var result = await service.SelectEmployeeAsync(
            new SelectEmployeeRequest(organization.Id, employee.Id),
            "previous-token",
            CancellationToken.None);

        Assert.Equal(employee.Id, result.Actor.Id);
        Assert.Equal("previous-token", store.ReplacedToken);
        Assert.Equal("EmployeeSelected", store.AuditEntry?.Action);
        Assert.Equal(employee.Id, store.AuditEntry?.ActorId);
        Assert.Equal(Now, store.AuditEntry?.OccurredAtUtc);
    }

    [Fact]
    public async Task Employee_cannot_use_administration_use_case()
    {
        var employeeActor = new CurrentActor(
            ActorKind.Employee,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pracownik",
            Permissions.For(ActorKind.Employee),
            Guid.NewGuid());
        var service = CreateService(new IdentityAccessStoreStub(), employeeActor);

        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            service.ListAdministratorsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Regular_administrator_cannot_manage_administrators_but_can_manage_organizations()
    {
        var administratorActor = new CurrentActor(
            ActorKind.Administrator,
            Guid.NewGuid(),
            null,
            "Administrator",
            Permissions.For(ActorKind.Administrator),
            Guid.NewGuid());
        var service = CreateService(new ListingIdentityStore(), administratorActor);

        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            service.ListAdministratorsAsync(CancellationToken.None));
        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            service.CreateAdministratorAsync(
                new CreateAdministratorRequest("next-admin", "Next Admin"),
                CancellationToken.None));
        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            service.UpdateAdministratorAsync(
                Guid.NewGuid(),
                new UpdateAdministratorRequest("updated-admin", "Updated Admin"),
                CancellationToken.None));
        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            service.ResetAdministratorPasswordAsync(
                Guid.NewGuid(),
                new ResetAdministratorPasswordRequest("secure-password"),
                CancellationToken.None));
        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            service.SetAdministratorActiveAsync(
                Guid.NewGuid(),
                new SetActiveRequest(false),
                CancellationToken.None));
        Assert.Empty(await service.ListOrganizationsAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Administrator_session_receives_management_permissions_from_bootstrap_status(
        bool isBootstrapAdministrator,
        bool canManageAdministrators)
    {
        var administrator = Administrator.Create(
            "admin",
            "Administrator",
            "hash",
            isBootstrapAdministrator,
            Now);
        var service = CreateService(new SigningInStore(administrator), actor: null);

        var session = await service.SignInAdministratorAsync(
            new AdministratorSignInRequest("admin", "secure-password"),
            null,
            CancellationToken.None);

        Assert.Contains(Permissions.IdentityManage, session.Actor.Permissions);
        Assert.Equal(
            canManageAdministrators,
            session.Actor.Permissions.Contains(Permissions.AdministratorsManage));
    }

    [Fact]
    public async Task Startup_provisioning_creates_an_audited_bootstrap_administrator()
    {
        var store = new BootstrapProvisioningStore(StoreMutationResult.Success, false);
        var service = CreateService(store, actor: null);

        var result = await service.EnsureBootstrapAdministratorAsync(
            new BootstrapAdministratorRequest("admin", "Administrator", "secure-password"),
            CancellationToken.None);

        Assert.Equal(BootstrapAdministratorProvisioningResult.Created, result);
        Assert.NotNull(store.CreatedAdministrator);
        Assert.True(store.CreatedAdministrator.IsBootstrapAdministrator);
        Assert.Equal("hash", store.CreatedAdministrator.PasswordHash);
        Assert.Equal("AdministratorBootstrapped", store.AuditEntry?.Action);
        Assert.Equal(Now, store.AuditEntry?.OccurredAtUtc);
    }

    [Fact]
    public async Task Startup_provisioning_does_not_modify_an_existing_bootstrap_administrator()
    {
        var store = new BootstrapProvisioningStore(StoreMutationResult.Success, true);
        var service = CreateService(store, actor: null);

        var result = await service.EnsureBootstrapAdministratorAsync(
            new BootstrapAdministratorRequest("ignored", "Ignored", "short"),
            CancellationToken.None);

        Assert.Equal(BootstrapAdministratorProvisioningResult.AlreadyExists, result);
        Assert.Null(store.CreatedAdministrator);
    }

    [Fact]
    public async Task Root_created_administrator_must_change_password_and_receives_no_permissions_on_first_sign_in()
    {
        var rootActor = new CurrentActor(
            ActorKind.Administrator,
            Guid.NewGuid(),
            null,
            "Root",
            Permissions.For(ActorKind.Administrator, true),
            Guid.NewGuid());
        var store = new CreatingAdministratorStore();
        var service = CreateService(store, rootActor);

        var created = await service.CreateAdministratorAsync(
            new CreateAdministratorRequest("new-admin", "New Admin"),
            CancellationToken.None);

        Assert.True(created.Administrator.RequiresPasswordChange);
        Assert.Equal("temporary-password", created.TemporaryPassword);
        Assert.True(store.CreatedAdministrator!.RequiresPasswordChange);

        var signInService = CreateService(new SigningInStore(store.CreatedAdministrator), actor: null);
        var session = await signInService.SignInAdministratorAsync(
            new AdministratorSignInRequest("new-admin", "temporary-password"),
            null,
            CancellationToken.None);

        Assert.True(session.Actor.RequiresPasswordChange);
        Assert.Empty(session.Actor.Permissions);
    }

    [Fact]
    public async Task Administrator_can_replace_temporary_password_and_clear_requirement()
    {
        var administrator = Administrator.Create("admin", "Admin", "old-hash", false, Now, requiresPasswordChange: true);
        var actor = new CurrentActor(
            ActorKind.Administrator,
            administrator.Id,
            null,
            administrator.DisplayName,
            [],
            Guid.NewGuid(),
            RequiresPasswordChange: true);
        var store = new ChangingPasswordStore(administrator);
        var service = CreateService(store, actor, new ComparingPasswordService());

        await service.ChangeOwnPasswordAsync(
            new ChangeOwnPasswordRequest("temporary-password", "new-secure-password"),
            CancellationToken.None);

        Assert.False(administrator.RequiresPasswordChange);
        Assert.Equal("new-hash", administrator.PasswordHash);
        Assert.Equal(actor.SessionId, store.CurrentSessionId);
        Assert.Equal("AdministratorPasswordChanged", store.AuditEntry?.Action);
    }

    [Fact]
    public async Task Concurrent_startup_accepts_a_bootstrap_administrator_created_by_another_instance()
    {
        var store = new BootstrapProvisioningStore(StoreMutationResult.Conflict, false, true);
        var service = CreateService(store, actor: null);

        var result = await service.EnsureBootstrapAdministratorAsync(
            new BootstrapAdministratorRequest("admin", "Administrator", "secure-password"),
            CancellationToken.None);

        Assert.Equal(BootstrapAdministratorProvisioningResult.AlreadyExists, result);
    }

    private static IdentityAccessService CreateService(
        IIdentityAccessStore store,
        CurrentActor? actor,
        IPasswordService? passwordService = null) =>
        new(
            store,
            passwordService ?? new PasswordServiceStub(),
            new TemporaryPasswordGeneratorStub(),
            new IdentityConfigurationStub(),
            new ApplicationAuthorizationService(new CurrentActorAccessorStub(actor)),
            new AuditEntryFactory(),
            new FixedTimeProvider(Now));

    private sealed class SelectingEmployeeStore : IdentityAccessStoreStub
    {
        private readonly Organization _organization;
        private readonly Employee _employee;

        public SelectingEmployeeStore(Organization organization, Employee employee)
        {
            _organization = organization;
            _employee = employee;
        }

        public string? ReplacedToken { get; private set; }
        public AuditEntry? AuditEntry { get; private set; }

        public override Task<Organization?> FindOrganizationAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Organization?>(id == _organization.Id ? _organization : null);

        public override Task<Employee?> FindEmployeeAsync(Guid organizationId, Guid employeeId, CancellationToken cancellationToken) =>
            Task.FromResult<Employee?>(
                organizationId == _organization.Id && employeeId == _employee.Id ? _employee : null);

        public override Task<SessionIssued> CreateSessionAsync(
            ActorKind actorKind,
            Guid actorId,
            Guid? organizationId,
            string actorDisplayName,
            IReadOnlyList<string> actorPermissions,
            TimeSpan lifetime,
            string? replacedToken,
            AuditEntry auditEntry,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ReplacedToken = replacedToken;
            AuditEntry = auditEntry;
            var actor = new CurrentActor(actorKind, actorId, organizationId, actorDisplayName, actorPermissions, Guid.NewGuid());
            return Task.FromResult(new SessionIssued("new-token", now.Add(lifetime), actor));
        }
    }

    private sealed class ListingIdentityStore : IdentityAccessStoreStub
    {
        public override Task<IReadOnlyList<Organization>> ListOrganizationsAsync(
            bool activeOnly,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Organization>>([]);
    }

    private sealed class CreatingAdministratorStore : IdentityAccessStoreStub
    {
        public Administrator? CreatedAdministrator { get; private set; }

        public override Task<StoreMutationResult> CreateAdministratorAsync(
            Administrator administrator,
            AuditEntry auditEntry,
            CancellationToken cancellationToken)
        {
            CreatedAdministrator = administrator;
            return Task.FromResult(StoreMutationResult.Success);
        }
    }

    private sealed class ChangingPasswordStore(Administrator administrator) : IdentityAccessStoreStub
    {
        public Guid? CurrentSessionId { get; private set; }
        public AuditEntry? AuditEntry { get; private set; }

        public override Task<Administrator?> FindAdministratorAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Administrator?>(id == administrator.Id ? administrator : null);

        public override Task<StoreMutationResult> ChangeAdministratorPasswordAsync(
            Administrator changedAdministrator,
            Guid currentSessionId,
            AuditEntry auditEntry,
            CancellationToken cancellationToken)
        {
            CurrentSessionId = currentSessionId;
            AuditEntry = auditEntry;
            return Task.FromResult(StoreMutationResult.Success);
        }
    }

    private sealed class SigningInStore : IdentityAccessStoreStub
    {
        private readonly Administrator _administrator;

        public SigningInStore(Administrator administrator)
        {
            _administrator = administrator;
        }

        public override Task<Administrator?> FindAdministratorByUsernameAsync(
            string normalizedUsername,
            CancellationToken cancellationToken) =>
            Task.FromResult<Administrator?>(_administrator);

        public override Task<SessionIssued> CreateSessionAsync(
            ActorKind actorKind,
            Guid actorId,
            Guid? organizationId,
            string actorDisplayName,
            IReadOnlyList<string> actorPermissions,
            TimeSpan lifetime,
            string? replacedToken,
            AuditEntry auditEntry,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var actor = new CurrentActor(
                actorKind,
                actorId,
                organizationId,
                actorDisplayName,
                actorPermissions,
                Guid.NewGuid(),
                _administrator.RequiresPasswordChange);
            return Task.FromResult(new SessionIssued("token", now.Add(lifetime), actor));
        }
    }

    private sealed class BootstrapProvisioningStore : IdentityAccessStoreStub
    {
        private readonly Queue<bool> _existenceResults;
        private readonly StoreMutationResult _createResult;

        public BootstrapProvisioningStore(
            StoreMutationResult createResult,
            params bool[] existenceResults)
        {
            _createResult = createResult;
            _existenceResults = new Queue<bool>(existenceResults);
        }

        public Administrator? CreatedAdministrator { get; private set; }
        public AuditEntry? AuditEntry { get; private set; }

        public override Task<bool> BootstrapAdministratorExistsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_existenceResults.Dequeue());

        public override Task<StoreMutationResult> CreateAdministratorAsync(
            Administrator administrator,
            AuditEntry auditEntry,
            CancellationToken cancellationToken)
        {
            CreatedAdministrator = administrator;
            AuditEntry = auditEntry;
            return Task.FromResult(_createResult);
        }
    }

    private class IdentityAccessStoreStub : IIdentityAccessStore
    {
        protected static Task<T> NotImplemented<T>() => Task.FromException<T>(new NotImplementedException());

        public virtual Task<bool> BootstrapAdministratorExistsAsync(CancellationToken cancellationToken) => NotImplemented<bool>();
        public virtual Task<Administrator?> FindAdministratorByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken) => NotImplemented<Administrator?>();
        public virtual Task<Administrator?> FindAdministratorAsync(Guid id, CancellationToken cancellationToken) => NotImplemented<Administrator?>();
        public virtual Task<IReadOnlyList<Administrator>> ListAdministratorsAsync(CancellationToken cancellationToken) => NotImplemented<IReadOnlyList<Administrator>>();
        public virtual Task<StoreMutationResult> CreateAdministratorAsync(Administrator administrator, AuditEntry auditEntry, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<StoreMutationResult> UpdateAdministratorAsync(Administrator administrator, AuditEntry auditEntry, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<StoreMutationResult> ResetAdministratorPasswordAsync(Administrator administrator, AuditEntry auditEntry, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<StoreMutationResult> ChangeAdministratorPasswordAsync(Administrator administrator, Guid currentSessionId, AuditEntry auditEntry, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<StoreMutationResult> SetAdministratorActiveAsync(Guid administratorId, bool isActive, AuditEntry auditEntry, DateTimeOffset now, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<IReadOnlyList<Organization>> ListOrganizationsAsync(bool activeOnly, CancellationToken cancellationToken) => NotImplemented<IReadOnlyList<Organization>>();
        public virtual Task<Organization?> FindOrganizationAsync(Guid id, CancellationToken cancellationToken) => NotImplemented<Organization?>();
        public virtual Task<StoreMutationResult> CreateOrganizationAsync(Organization organization, AuditEntry auditEntry, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<StoreMutationResult> UpdateOrganizationAsync(Organization organization, AuditEntry auditEntry, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<StoreMutationResult> SetOrganizationActiveAsync(Guid organizationId, bool isActive, AuditEntry auditEntry, DateTimeOffset now, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<IReadOnlyList<Employee>> ListEmployeesAsync(Guid organizationId, bool activeOnly, CancellationToken cancellationToken) => NotImplemented<IReadOnlyList<Employee>>();
        public virtual Task<Employee?> FindEmployeeAsync(Guid organizationId, Guid employeeId, CancellationToken cancellationToken) => NotImplemented<Employee?>();
        public virtual Task<StoreMutationResult> CreateEmployeeAsync(Employee employee, AuditEntry auditEntry, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<StoreMutationResult> UpdateEmployeeAsync(Employee employee, AuditEntry auditEntry, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<StoreMutationResult> SetEmployeeActiveAsync(Guid organizationId, Guid employeeId, bool isActive, AuditEntry auditEntry, DateTimeOffset now, CancellationToken cancellationToken) => NotImplemented<StoreMutationResult>();
        public virtual Task<SessionIssued> CreateSessionAsync(ActorKind actorKind, Guid actorId, Guid? organizationId, string actorDisplayName, IReadOnlyList<string> actorPermissions, TimeSpan lifetime, string? replacedToken, AuditEntry auditEntry, DateTimeOffset now, CancellationToken cancellationToken) => NotImplemented<SessionIssued>();
        public virtual Task<CurrentActor?> ResolveSessionAsync(string token, DateTimeOffset now, CancellationToken cancellationToken) => NotImplemented<CurrentActor?>();
        public virtual Task RevokeSessionAsync(string token, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromException(new NotImplementedException());
    }

    private sealed class PasswordServiceStub : IPasswordService
    {
        public string Hash(Administrator administrator, string password) => "hash";
        public bool Verify(Administrator administrator, string password) => true;
    }

    private sealed class ComparingPasswordService : IPasswordService
    {
        public string Hash(Administrator administrator, string password) => "new-hash";
        public bool Verify(Administrator administrator, string password) => password == "temporary-password";
    }

    private sealed class TemporaryPasswordGeneratorStub : ITemporaryPasswordGenerator
    {
        public string Generate() => "temporary-password";
    }

    private sealed class IdentityConfigurationStub : IIdentityConfiguration
    {
        public TimeSpan AdministratorSessionLifetime => TimeSpan.FromHours(8);
        public TimeSpan EmployeeSessionLifetime => TimeSpan.FromHours(12);
        public bool IsValidBootstrapToken(string token) => true;
    }

    private sealed class CurrentActorAccessorStub(CurrentActor? actor) : ICurrentActorAccessor
    {
        public CurrentActor? Actor { get; } = actor;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
