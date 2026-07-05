using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubiektMobile.Application.Identity;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Infrastructure.Identity;
using SubiektMobile.Infrastructure.Persistence.Application;
using Xunit;

namespace SubiektMobile.Infrastructure.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlIdentityCollection
{
    public const string Name = "PostgreSQL identity";
}

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SUBIEKT_MOBILE_TEST_DB")))
        {
            Skip = "Set SUBIEKT_MOBILE_TEST_DB to run PostgreSQL integration tests.";
        }
    }
}

[Collection(PostgreSqlIdentityCollection.Name)]
public sealed class IdentityAccessStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    [PostgreSqlFact]
    public async Task Concurrent_bootstrap_creates_exactly_one_bootstrap_administrator()
    {
        var connectionString = GetTestConnectionString();

        await ResetDatabaseAsync(connectionString);
        await using var firstContext = CreateContext(connectionString);
        await using var secondContext = CreateContext(connectionString);
        var firstStore = new IdentityAccessStore(firstContext);
        var secondStore = new IdentityAccessStore(secondContext);
        var first = Administrator.Create("admin-one", "Admin One", "hash", true, Now);
        var second = Administrator.Create("admin-two", "Admin Two", "hash", true, Now);

        var results = await Task.WhenAll(
            firstStore.CreateAdministratorAsync(first, SystemAudit(first.Id), CancellationToken.None),
            secondStore.CreateAdministratorAsync(second, SystemAudit(second.Id), CancellationToken.None));

        Assert.Single(results, x => x == StoreMutationResult.Success);
        Assert.Single(results, x => x == StoreMutationResult.Conflict);
        await using var verificationContext = CreateContext(connectionString);
        Assert.Equal(1, await verificationContext.Administrators.CountAsync(x => x.IsBootstrapAdministrator));
    }

    [PostgreSqlFact]
    public async Task Deactivating_administrator_immediately_invalidates_existing_session()
    {
        var connectionString = GetTestConnectionString();

        await ResetDatabaseAsync(connectionString);
        await using var context = CreateContext(connectionString);
        var store = new IdentityAccessStore(context);
        var bootstrap = Administrator.Create("bootstrap", "Bootstrap Admin", "hash", true, Now);
        var target = Administrator.Create("target", "Target Admin", "hash", false, Now);
        Assert.Equal(
            StoreMutationResult.Success,
            await store.CreateAdministratorAsync(bootstrap, SystemAudit(bootstrap.Id), CancellationToken.None));
        Assert.Equal(
            StoreMutationResult.Success,
            await store.CreateAdministratorAsync(target, SystemAudit(target.Id), CancellationToken.None));

        var session = await store.CreateSessionAsync(
            ActorKind.Administrator,
            target.Id,
            null,
            target.DisplayName,
            Permissions.For(ActorKind.Administrator),
            TimeSpan.FromHours(8),
            null,
            ActorAudit(target, "AdministratorSignedIn"),
            Now,
            CancellationToken.None);
        Assert.NotNull(await store.ResolveSessionAsync(session.Token, Now, CancellationToken.None));

        var result = await store.SetAdministratorActiveAsync(
            target.Id,
            false,
            ActorAudit(bootstrap, "AdministratorDeactivated", target.Id),
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.Equal(StoreMutationResult.Success, result);
        Assert.Null(await store.ResolveSessionAsync(session.Token, Now.AddMinutes(1), CancellationToken.None));
    }

    [PostgreSqlFact]
    public async Task Last_active_administrator_cannot_be_deactivated()
    {
        var connectionString = GetTestConnectionString();

        await ResetDatabaseAsync(connectionString);
        await using var context = CreateContext(connectionString);
        var store = new IdentityAccessStore(context);
        var administrator = Administrator.Create("only-admin", "Only Admin", "hash", true, Now);
        Assert.Equal(
            StoreMutationResult.Success,
            await store.CreateAdministratorAsync(administrator, SystemAudit(administrator.Id), CancellationToken.None));

        var result = await store.SetAdministratorActiveAsync(
            administrator.Id,
            false,
            ActorAudit(administrator, "AdministratorDeactivated"),
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.Equal(StoreMutationResult.LastActiveAdministrator, result);
        Assert.True((await context.Administrators.AsNoTracking().SingleAsync()).IsActive);
    }

    [PostgreSqlFact]
    public async Task Password_reset_invalidates_all_administrator_sessions()
    {
        var connectionString = GetTestConnectionString();

        await ResetDatabaseAsync(connectionString);
        await using var context = CreateContext(connectionString);
        var store = new IdentityAccessStore(context);
        var administrator = Administrator.Create("reset-admin", "Reset Admin", "old-hash", true, Now);
        await store.CreateAdministratorAsync(administrator, SystemAudit(administrator.Id), CancellationToken.None);
        var session = await store.CreateSessionAsync(
            ActorKind.Administrator,
            administrator.Id,
            null,
            administrator.DisplayName,
            Permissions.For(ActorKind.Administrator, administrator.IsBootstrapAdministrator),
            TimeSpan.FromHours(8),
            null,
            ActorAudit(administrator, "AdministratorSignedIn"),
            Now,
            CancellationToken.None);

        administrator.SetPasswordHash("new-hash", Now.AddMinutes(1));
        var result = await store.ResetAdministratorPasswordAsync(
            administrator,
            ActorAudit(administrator, "AdministratorPasswordReset"),
            CancellationToken.None);

        Assert.Equal(StoreMutationResult.Success, result);
        Assert.Null(await store.ResolveSessionAsync(session.Token, Now.AddMinutes(1), CancellationToken.None));
    }

    [PostgreSqlFact]
    public async Task Resolving_sessions_rebuilds_root_and_regular_administrator_permissions()
    {
        var connectionString = GetTestConnectionString();
        await ResetDatabaseAsync(connectionString);
        await using var context = CreateContext(connectionString);
        var store = new IdentityAccessStore(context);
        var root = Administrator.Create("root", "Root", "hash", true, Now);
        var regular = Administrator.Create("admin", "Admin", "hash", false, Now);
        await store.CreateAdministratorAsync(root, SystemAudit(root.Id), CancellationToken.None);
        await store.CreateAdministratorAsync(regular, SystemAudit(regular.Id), CancellationToken.None);

        var rootSession = await store.CreateSessionAsync(
            ActorKind.Administrator, root.Id, null, root.DisplayName,
            Permissions.For(ActorKind.Administrator, true), TimeSpan.FromHours(8), null,
            ActorAudit(root, "AdministratorSignedIn"), Now, CancellationToken.None);
        var regularSession = await store.CreateSessionAsync(
            ActorKind.Administrator, regular.Id, null, regular.DisplayName,
            Permissions.For(ActorKind.Administrator), TimeSpan.FromHours(8), null,
            ActorAudit(regular, "AdministratorSignedIn"), Now, CancellationToken.None);

        var resolvedRoot = await store.ResolveSessionAsync(rootSession.Token, Now, CancellationToken.None);
        var resolvedRegular = await store.ResolveSessionAsync(regularSession.Token, Now, CancellationToken.None);

        Assert.Contains(Permissions.AdministratorsManage, resolvedRoot!.Permissions);
        Assert.DoesNotContain(Permissions.AdministratorsManage, resolvedRegular!.Permissions);
    }

    private static string GetTestConnectionString()
    {
        var value = Environment.GetEnvironmentVariable("SUBIEKT_MOBILE_TEST_DB");
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("SUBIEKT_MOBILE_TEST_DB is required for this test.");
        }

        var builder = new NpgsqlConnectionStringBuilder(value);
        if (builder.Database is null || !builder.Database.EndsWith("_tests", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SUBIEKT_MOBILE_TEST_DB must point to a dedicated database whose name ends with '_tests'.");
        }

        return value;
    }

    private static async Task ResetDatabaseAsync(string connectionString)
    {
        await using var context = CreateContext(connectionString);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    private static ApplicationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AuditEntry SystemAudit(Guid targetId) =>
        AuditEntry.Create(ActorKind.System, null, null, "System", "AdministratorBootstrapped", "Administrator", targetId, Now);

    private static AuditEntry ActorAudit(Administrator administrator, string action, Guid? targetId = null) =>
        AuditEntry.Create(
            ActorKind.Administrator,
            administrator.Id,
            null,
            administrator.DisplayName,
            action,
            "Administrator",
            targetId ?? administrator.Id,
            Now);
}
