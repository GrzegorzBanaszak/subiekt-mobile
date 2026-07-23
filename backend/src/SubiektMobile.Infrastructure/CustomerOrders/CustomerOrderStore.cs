using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubiektMobile.Application.CustomerOrders;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.CustomerOrders;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;
using SubiektMobile.Infrastructure.Persistence.Application;

namespace SubiektMobile.Infrastructure.CustomerOrders;

public sealed class CustomerOrderStore(ApplicationDbContext db) : ICustomerOrderStore
{
    public async Task<PagedResult<CustomerOrderListItemDto>> ListAsync(string? search, CustomerOrderStatus? status,
        Guid? customerId, Guid? customerSiteId, DateOnly? dueDateFrom, DateOnly? dueDateTo, int page, int pageSize,
        CancellationToken ct)
    {
        var query = from order in db.CustomerOrders.AsNoTracking()
            join customer in db.Customers.AsNoTracking() on order.CustomerId equals customer.Id
            join site in db.CustomerSites.AsNoTracking() on order.CustomerSiteId equals site.Id
            select new { order, customer.Name, SiteName = site.Name };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var phrase = search.Trim().ToUpperInvariant();
            query = query.Where(x => (x.order.CustomerOrderNumber ?? "").ToUpper().Contains(phrase)
                || x.Name.ToUpper().Contains(phrase) || x.SiteName.ToUpper().Contains(phrase));
        }
        if (status is not null) query = query.Where(x => x.order.Status == status);
        if (customerId is not null) query = query.Where(x => x.order.CustomerId == customerId);
        if (customerSiteId is not null) query = query.Where(x => x.order.CustomerSiteId == customerSiteId);
        if (dueDateFrom is not null) query = query.Where(x => x.order.RequestedDeliveryDate >= dueDateFrom);
        if (dueDateTo is not null) query = query.Where(x => x.order.RequestedDeliveryDate <= dueDateTo);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.order.RequestedDeliveryDate).ThenByDescending(x => x.order.UpdatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerOrderListItemDto(x.order.Id, x.order.CustomerOrderNumber, x.Name, x.SiteName,
                x.order.RequestedDeliveryDate, x.order.Status, x.order.Items.Count, x.order.UpdatedAtUtc,
                x.order.Version)).ToListAsync(ct);
        return Page(items, page, pageSize, total);
    }

    public Task<CustomerOrder?> FindAsync(Guid id, bool tracking, CancellationToken ct)
    {
        IQueryable<CustomerOrder> query = db.CustomerOrders.Include(x => x.Items);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Guid?> FindWarehouseOrderIdAsync(Guid customerOrderId, CancellationToken ct) =>
        db.WarehouseOrders.AsNoTracking().Where(x => x.CustomerOrderId == customerOrderId)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);

    public async Task<PagedResult<CustomerOrderActivityDto>> ListActivityAsync(Guid customerOrderId, int page,
        int pageSize, CancellationToken ct)
    {
        var query = db.AuditEntries.AsNoTracking().Where(x =>
            (x.TargetType == "CustomerOrder" && x.TargetId == customerOrderId)
            || (x.TargetType == "CustomerOrderItem" && db.CustomerOrderItems
                .Where(item => item.CustomerOrderId == customerOrderId).Select(item => item.Id).Contains(x.TargetId!.Value)));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerOrderActivityDto(x.Id, x.Action, x.ActorKind, x.ActorDisplayName, x.OccurredAtUtc))
            .ToListAsync(ct);
        return Page(items, page, pageSize, total);
    }

    public async Task<CustomerOrderStoreResult> AddAsync(CustomerOrder order, AuditEntry audit, CancellationToken ct)
    {
        db.CustomerOrders.Add(order);
        db.AuditEntries.Add(audit);
        return await Save(ct);
    }

    public async Task<CustomerOrderStoreResult> SaveAsync(CustomerOrder order, long expectedVersion, AuditEntry audit,
        CancellationToken ct)
    {
        db.Entry(order).Property(x => x.Version).OriginalValue = expectedVersion;
        db.AuditEntries.Add(audit);
        return await Save(ct);
    }

    public async Task<CustomerOrderStoreResult> ConvertAsync(CustomerOrder order, WarehouseOrder warehouseOrder,
        long expectedOrderVersion, AuditEntry customerOrderAudit, AuditEntry warehouseOrderAudit,
        CancellationToken ct)
    {
        db.Entry(order).Property(x => x.Version).OriginalValue = expectedOrderVersion;
        db.WarehouseOrders.Add(warehouseOrder);
        db.AuditEntries.Add(customerOrderAudit);
        db.AuditEntries.Add(warehouseOrderAudit);
        return await Save(ct);
    }

    private async Task<CustomerOrderStoreResult> Save(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return CustomerOrderStoreResult.Success;
        }
        catch (DbUpdateConcurrencyException) { return CustomerOrderStoreResult.Conflict; }
        catch (DbUpdateException ex) when (FindPostgres(ex)?.SqlState == PostgresErrorCodes.UniqueViolation)
        { return CustomerOrderStoreResult.Conflict; }
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

    private static PagedResult<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize, int total) =>
        new(items, page, pageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
}
