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
        Assert.DoesNotContain(Permissions.OrdersManage, permissions);
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
    public async Task Concurrent_startup_accepts_a_bootstrap_administrator_created_by_another_instance()
    {
        var store = new BootstrapProvisioningStore(StoreMutationResult.Conflict, false, true);
        var service = CreateService(store, actor: null);

        var result = await service.EnsureBootstrapAdministratorAsync(
            new BootstrapAdministratorRequest("admin", "Administrator", "secure-password"),
            CancellationToken.None);

        Assert.Equal(BootstrapAdministratorProvisioningResult.AlreadyExists, result);
    }

    private static IdentityAccessService CreateService(IIdentityAccessStore store, CurrentActor? actor) =>
        new(
            store,
            new PasswordServiceStub(),
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
            TimeSpan lifetime,
            string? replacedToken,
            AuditEntry auditEntry,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ReplacedToken = replacedToken;
            AuditEntry = auditEntry;
            var actor = new CurrentActor(actorKind, actorId, organizationId, actorDisplayName, Permissions.For(actorKind), Guid.NewGuid());
            return Task.FromResult(new SessionIssued("new-token", now.Add(lifetime), actor));
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
        public virtual Task<SessionIssued> CreateSessionAsync(ActorKind actorKind, Guid actorId, Guid? organizationId, string actorDisplayName, TimeSpan lifetime, string? replacedToken, AuditEntry auditEntry, DateTimeOffset now, CancellationToken cancellationToken) => NotImplemented<SessionIssued>();
        public virtual Task<CurrentActor?> ResolveSessionAsync(string token, DateTimeOffset now, CancellationToken cancellationToken) => NotImplemented<CurrentActor?>();
        public virtual Task RevokeSessionAsync(string token, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromException(new NotImplementedException());
    }

    private sealed class PasswordServiceStub : IPasswordService
    {
        public string Hash(Administrator administrator, string password) => "hash";
        public bool Verify(Administrator administrator, string password) => true;
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
