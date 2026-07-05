using System.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubiektMobile.Application.Identity;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Infrastructure.Persistence.Application;
using SubiektMobile.Infrastructure.Persistence.Application.Entities;

namespace SubiektMobile.Infrastructure.Identity;

public sealed class IdentityAccessStore : IIdentityAccessStore
{
    private const string UniqueViolation = "23505";
    private const string SerializationFailure = "40001";
    private readonly ApplicationDbContext _dbContext;

    public IdentityAccessStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> BootstrapAdministratorExistsAsync(CancellationToken cancellationToken) =>
        _dbContext.Administrators
            .AsNoTracking()
            .AnyAsync(x => x.IsBootstrapAdministrator, cancellationToken);

    public Task<Administrator?> FindAdministratorByUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken) =>
        _dbContext.Administrators.SingleOrDefaultAsync(
            x => x.NormalizedUsername == normalizedUsername,
            cancellationToken);

    public Task<Administrator?> FindAdministratorAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Administrators.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Administrator>> ListAdministratorsAsync(CancellationToken cancellationToken) =>
        await _dbContext.Administrators
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .ToListAsync(cancellationToken);

    public async Task<StoreMutationResult> CreateAdministratorAsync(
        Administrator administrator,
        AuditEntry auditEntry,
        CancellationToken cancellationToken)
    {
        _dbContext.Administrators.Add(administrator);
        _dbContext.AuditEntries.Add(auditEntry);
        return await SaveWithConflictMappingAsync(cancellationToken);
    }

    public async Task<StoreMutationResult> UpdateAdministratorAsync(
        Administrator administrator,
        AuditEntry auditEntry,
        CancellationToken cancellationToken)
    {
        _dbContext.AuditEntries.Add(auditEntry);
        return await SaveWithConflictMappingAsync(cancellationToken);
    }

    public async Task<StoreMutationResult> ResetAdministratorPasswordAsync(
        Administrator administrator,
        AuditEntry auditEntry,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.AuditEntries.Add(auditEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await RevokeAdministratorSessionsAsync(administrator.Id, auditEntry.OccurredAtUtc, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StoreMutationResult.Success;
    }

    public async Task<StoreMutationResult> SetAdministratorActiveAsync(
        Guid administratorId,
        bool isActive,
        AuditEntry auditEntry,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var administrator = await _dbContext.Administrators.SingleOrDefaultAsync(
                x => x.Id == administratorId,
                cancellationToken);
            if (administrator is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StoreMutationResult.NotFound;
            }

            if (!isActive && administrator.IsActive)
            {
                var activeCount = await _dbContext.Administrators.CountAsync(x => x.IsActive, cancellationToken);
                if (activeCount <= 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return StoreMutationResult.LastActiveAdministrator;
                }
            }

            administrator.SetActive(isActive, now);
            _dbContext.AuditEntries.Add(auditEntry);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (!isActive)
            {
                await RevokeAdministratorSessionsAsync(administratorId, now, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return StoreMutationResult.Success;
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return StoreMutationResult.Conflict;
        }
    }

    public async Task<IReadOnlyList<Organization>> ListOrganizationsAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Organizations.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Organization?> FindOrganizationAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Organizations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<StoreMutationResult> CreateOrganizationAsync(
        Organization organization,
        AuditEntry auditEntry,
        CancellationToken cancellationToken)
    {
        _dbContext.Organizations.Add(organization);
        _dbContext.AuditEntries.Add(auditEntry);
        return await SaveWithConflictMappingAsync(cancellationToken);
    }

    public async Task<StoreMutationResult> UpdateOrganizationAsync(
        Organization organization,
        AuditEntry auditEntry,
        CancellationToken cancellationToken)
    {
        _dbContext.AuditEntries.Add(auditEntry);
        return await SaveWithConflictMappingAsync(cancellationToken);
    }

    public async Task<StoreMutationResult> SetOrganizationActiveAsync(
        Guid organizationId,
        bool isActive,
        AuditEntry auditEntry,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var organization = await _dbContext.Organizations.SingleOrDefaultAsync(
            x => x.Id == organizationId,
            cancellationToken);
        if (organization is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StoreMutationResult.NotFound;
        }

        organization.SetActive(isActive, now);
        _dbContext.AuditEntries.Add(auditEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (!isActive)
        {
            await _dbContext.AuthenticationSessions
                .Where(x => x.OrganizationId == organizationId && x.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.RevokedAtUtc, now),
                    cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return StoreMutationResult.Success;
    }

    public async Task<IReadOnlyList<Employee>> ListEmployeesAsync(
        Guid organizationId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Employees
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query.OrderBy(x => x.DisplayName).ThenBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<Employee?> FindEmployeeAsync(
        Guid organizationId,
        Guid employeeId,
        CancellationToken cancellationToken) =>
        _dbContext.Employees.SingleOrDefaultAsync(
            x => x.OrganizationId == organizationId && x.Id == employeeId,
            cancellationToken);

    public async Task<StoreMutationResult> CreateEmployeeAsync(
        Employee employee,
        AuditEntry auditEntry,
        CancellationToken cancellationToken)
    {
        _dbContext.Employees.Add(employee);
        _dbContext.AuditEntries.Add(auditEntry);
        return await SaveWithConflictMappingAsync(cancellationToken);
    }

    public async Task<StoreMutationResult> UpdateEmployeeAsync(
        Employee employee,
        AuditEntry auditEntry,
        CancellationToken cancellationToken)
    {
        _dbContext.AuditEntries.Add(auditEntry);
        return await SaveWithConflictMappingAsync(cancellationToken);
    }

    public async Task<StoreMutationResult> SetEmployeeActiveAsync(
        Guid organizationId,
        Guid employeeId,
        bool isActive,
        AuditEntry auditEntry,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(
            x => x.OrganizationId == organizationId && x.Id == employeeId,
            cancellationToken);
        if (employee is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StoreMutationResult.NotFound;
        }

        employee.SetActive(isActive, now);
        _dbContext.AuditEntries.Add(auditEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (!isActive)
        {
            await _dbContext.AuthenticationSessions
                .Where(x => x.EmployeeId == employeeId && x.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.RevokedAtUtc, now),
                    cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return StoreMutationResult.Success;
    }

    public async Task<SessionIssued> CreateSessionAsync(
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
        var rawToken = GenerateToken();
        var sessionId = Guid.NewGuid();
        var expiresAt = now.Add(lifetime);
        var session = new AuthenticationSession(
            sessionId,
            HashToken(rawToken),
            actorKind,
            actorKind == ActorKind.Administrator ? actorId : null,
            actorKind == ActorKind.Employee ? actorId : null,
            organizationId,
            now,
            expiresAt);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(replacedToken))
        {
            var replacedHash = HashToken(replacedToken);
            await _dbContext.AuthenticationSessions
                .Where(x => x.TokenHash == replacedHash && x.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.RevokedAtUtc, now),
                    cancellationToken);
        }

        _dbContext.AuthenticationSessions.Add(session);
        _dbContext.AuditEntries.Add(auditEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var actor = new CurrentActor(
            actorKind,
            actorId,
            organizationId,
            actorDisplayName,
            Permissions.For(actorKind),
            sessionId);
        return new SessionIssued(rawToken, expiresAt, actor);
    }

    public async Task<CurrentActor?> ResolveSessionAsync(
        string token,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = HashToken(token);
        var session = await _dbContext.AuthenticationSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash && x.RevokedAtUtc == null && x.ExpiresAtUtc > now,
                cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.ActorKind == ActorKind.Administrator && session.AdministratorId is Guid administratorId)
        {
            var administrator = await _dbContext.Administrators
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == administratorId && x.IsActive, cancellationToken);
            return administrator is null
                ? null
                : new CurrentActor(
                    ActorKind.Administrator,
                    administrator.Id,
                    null,
                    administrator.DisplayName,
                    Permissions.For(ActorKind.Administrator),
                    session.Id);
        }

        if (session.ActorKind == ActorKind.Employee
            && session.EmployeeId is Guid employeeId
            && session.OrganizationId is Guid organizationId)
        {
            var employee = await _dbContext.Employees
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == employeeId && x.OrganizationId == organizationId && x.IsActive,
                    cancellationToken);
            var organizationIsActive = employee is not null
                && await _dbContext.Organizations
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == organizationId && x.IsActive, cancellationToken);
            return employee is null || !organizationIsActive
                ? null
                : new CurrentActor(
                    ActorKind.Employee,
                    employee.Id,
                    organizationId,
                    employee.DisplayName,
                    Permissions.For(ActorKind.Employee),
                    session.Id);
        }

        return null;
    }

    public async Task RevokeSessionAsync(
        string token,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var tokenHash = HashToken(token);
        await _dbContext.AuthenticationSessions
            .Where(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevokedAtUtc, now),
                cancellationToken);
    }

    private async Task RevokeAdministratorSessionsAsync(
        Guid administratorId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await _dbContext.AuthenticationSessions
            .Where(x => x.AdministratorId == administratorId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevokedAtUtc, now),
                cancellationToken);

    private async Task<StoreMutationResult> SaveWithConflictMappingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return StoreMutationResult.Success;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return StoreMutationResult.Conflict;
        }
    }

    private static bool IsUniqueViolation(Exception exception) =>
        FindPostgresException(exception)?.SqlState == UniqueViolation;

    private static bool IsSerializationFailure(Exception exception) =>
        FindPostgresException(exception)?.SqlState == SerializationFailure;

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

            if (current.InnerException is null)
            {
                break;
            }
        }

        return null;
    }

    private static string GenerateToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return token.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
