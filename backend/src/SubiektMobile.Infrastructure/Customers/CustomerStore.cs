using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubiektMobile.Application.Customers;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Customers;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Infrastructure.Persistence;
using SubiektMobile.Infrastructure.Persistence.Application;

namespace SubiektMobile.Infrastructure.Customers;

public sealed class CustomerStore(ApplicationDbContext dbContext) : ICustomerStore
{
    public async Task<PagedResult<CustomerListItemDto>> ListAsync(string? search, bool? isActive, int page, int pageSize,
        CancellationToken ct)
    {
        var query = dbContext.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var phrase = search.Trim().ToUpper();
            query = query.Where(x => x.NormalizedCode.Contains(phrase) || x.Name.ToUpper().Contains(phrase)
                || (x.TaxId != null && x.TaxId.ToUpper().Contains(phrase)));
        }
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive.Value);
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerListItemDto(x.Id, x.Code, x.Name, x.TaxId, x.IsActive, x.Sites.Count,
                x.Sites.Count(site => site.LogisticsProfile != null
                    && site.LogisticsProfile.RecipientName != null && site.LogisticsProfile.Street != null
                    && site.LogisticsProfile.PostalCode != null && site.LogisticsProfile.City != null
                    && site.LogisticsProfile.DefaultDock != null && site.LogisticsProfile.SupplierNumber != null
                    && site.LogisticsProfile.LabelProfile != null), x.UpdatedAtUtc, x.Version))
            .ToListAsync(ct);
        return new PagedResult<CustomerListItemDto>(items, page, pageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize));
    }

    public Task<Customer?> FindAsync(Guid id, bool tracking, CancellationToken ct)
    {
        IQueryable<Customer> query = dbContext.Customers.Include(x => x.Sites).ThenInclude(x => x.LogisticsProfile);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<CustomerSite?> FindSiteAsync(Guid customerId, Guid siteId, bool tracking, CancellationToken ct)
    {
        IQueryable<CustomerSite> query = dbContext.CustomerSites.Include(x => x.LogisticsProfile);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.CustomerId == customerId && x.Id == siteId, ct);
    }

    public async Task<PagedResult<CustomerSiteListItemDto>> ListSitesAsync(Guid customerId, string? search, int page,
        int pageSize, CancellationToken ct)
    {
        var query = dbContext.CustomerSites.AsNoTracking().Where(x => x.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var phrase = search.Trim().ToUpper();
            query = query.Where(x => x.NormalizedCode.Contains(phrase) || x.Name.ToUpper().Contains(phrase));
        }
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerSiteListItemDto(x.Id, x.Code, x.Name, x.CountryCode, x.IsActive,
                x.LogisticsProfile == null ? null : x.LogisticsProfile.DefaultDock,
                x.LogisticsProfile == null ? null : x.LogisticsProfile.SupplierNumber,
                x.LogisticsProfile == null ? null : x.LogisticsProfile.LabelProfile,
                x.LogisticsProfile != null && x.LogisticsProfile.RecipientName != null
                    && x.LogisticsProfile.Street != null && x.LogisticsProfile.PostalCode != null
                    && x.LogisticsProfile.City != null && x.LogisticsProfile.DefaultDock != null
                    && x.LogisticsProfile.SupplierNumber != null && x.LogisticsProfile.LabelProfile != null,
                x.Version)).ToListAsync(ct);
        return new PagedResult<CustomerSiteListItemDto>(items, page, pageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize));
    }

    public async Task<PagedResult<CustomerActivityDto>> ListActivityAsync(Guid customerId, int page, int pageSize,
        CancellationToken ct)
    {
        var siteIds = await dbContext.CustomerSites.AsNoTracking().Where(x => x.CustomerId == customerId)
            .Select(x => x.Id).ToListAsync(ct);
        var query = dbContext.AuditEntries.AsNoTracking().Where(x =>
            (x.TargetType == "Customer" && x.TargetId == customerId)
            || (x.TargetType == "CustomerSite" && x.TargetId != null && siteIds.Contains(x.TargetId.Value)));
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerActivityDto(x.Id, x.Action, x.TargetType, x.TargetId, x.ActorKind,
                x.ActorDisplayName, x.OccurredAtUtc)).ToListAsync(ct);
        return new PagedResult<CustomerActivityDto>(items, page, pageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize));
    }

    public async Task<CustomerStoreResult> AddAsync(Customer customer, AuditEntry audit, CancellationToken ct)
    {
        dbContext.Customers.Add(customer);
        dbContext.AuditEntries.Add(audit);
        return await Save(ct);
    }

    public async Task<CustomerStoreResult> SaveCustomerAsync(Customer customer, long expectedVersion, AuditEntry audit,
        CancellationToken ct)
    {
        dbContext.Entry(customer).Property(x => x.Version).OriginalValue = expectedVersion;
        dbContext.AuditEntries.Add(audit);
        return await Save(ct);
    }

    public async Task<CustomerStoreResult> SaveSiteAsync(CustomerSite site, long expectedVersion, AuditEntry audit,
        CancellationToken ct)
    {
        dbContext.Entry(site).Property(x => x.Version).OriginalValue = expectedVersion;
        dbContext.AuditEntries.Add(audit);
        return await Save(ct);
    }

    private async Task<CustomerStoreResult> Save(CancellationToken ct)
    {
        try { await dbContext.SaveChangesAsync(ct); return CustomerStoreResult.Success; }
        catch (DbUpdateConcurrencyException) { return CustomerStoreResult.Conflict; }
        catch (DbUpdateException ex) when (FindPostgres(ex)?.SqlState == PostgresErrorCodes.UniqueViolation)
        { return CustomerStoreResult.Conflict; }
    }

    private static PostgresException? FindPostgres(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException postgres) return postgres;
            if (current.InnerException is null) break;
        }
        return null;
    }
}

public sealed class CustomerContractorDirectory(SubiektDbContext dbContext) : ICustomerContractorDirectory
{
    public async Task<PagedResult<CustomerContractorDto>> SearchAsync(string? search, int page, int pageSize,
        CancellationToken ct)
    {
        var query = from contractor in dbContext.Kontrahenci.AsNoTracking()
            join address in dbContext.Adresy.AsNoTracking().Where(x => x.AddressType == 1)
                on contractor.Id equals address.ObjectId into addresses
            from address in addresses.DefaultIfEmpty()
            where contractor.Zablokowany != true && (contractor.Rodzaj == 0 || contractor.Rodzaj == 2)
            select new ContractorDirectoryRow
            {
                Id = contractor.Id,
                Symbol = contractor.Symbol,
                Name = address.FullName ?? address.Name,
                TaxId = address.Nip
            };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var phrase = search.Trim();
            query = query.Where(x => (x.Symbol ?? "").Contains(phrase) || (x.Name ?? "").Contains(phrase)
                || (x.TaxId ?? "").Contains(phrase));
        }
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Symbol).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerContractorDto(x.Id, x.Symbol ?? x.Id.ToString(), x.Name ?? x.Symbol ?? x.Id.ToString(),
                x.TaxId)).ToListAsync(ct);
        return new PagedResult<CustomerContractorDto>(items, page, pageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize));
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct) => dbContext.Kontrahenci.AsNoTracking()
        .AnyAsync(x => x.Id == id && x.Zablokowany != true && (x.Rodzaj == 0 || x.Rodzaj == 2), ct);

    private sealed class ContractorDirectoryRow
    {
        public int Id { get; init; }
        public string? Symbol { get; init; }
        public string? Name { get; init; }
        public string? TaxId { get; init; }
    }
}
