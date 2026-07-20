using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubiektMobile.Application.WarehouseOrders;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;
using SubiektMobile.Infrastructure.Persistence.Application;

namespace SubiektMobile.Infrastructure.WarehouseOrders;

public sealed class WarehouseOrderStore(ApplicationDbContext dbContext) : IWarehouseOrderStore
{
    public async Task<PagedResult<WarehouseOrderListItemDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = dbContext.WarehouseOrders.AsNoTracking();
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Number)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new WarehouseOrderListItemDto(x.Id, x.Number, x.CustomerName, x.DueDate, x.Status,
                x.Items.Count, x.UpdatedAtUtc, x.Version)).ToListAsync(ct);
        return new PagedResult<WarehouseOrderListItemDto>(items, page, pageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize));
    }

    public Task<WarehouseOrder?> FindAsync(Guid id, bool tracking, CancellationToken ct)
    {
        IQueryable<WarehouseOrder> query = dbContext.WarehouseOrders.Include(x => x.Items).Include(x => x.Assignees);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<WarehouseOrderStoreResult> AddAsync(WarehouseOrder warehouseOrder, AuditEntry audit, CancellationToken ct)
    {
        dbContext.WarehouseOrders.Add(warehouseOrder);
        dbContext.AuditEntries.Add(audit);
        try { await dbContext.SaveChangesAsync(ct); return WarehouseOrderStoreResult.Success; }
        catch (DbUpdateException ex) when (FindPostgres(ex)?.SqlState == PostgresErrorCodes.UniqueViolation)
        { return WarehouseOrderStoreResult.Conflict; }
    }

    public async Task<WarehouseOrderStoreResult> SaveAsync(WarehouseOrder warehouseOrder, long expectedVersion, AuditEntry audit, CancellationToken ct)
    {
        foreach (var item in warehouseOrder.Items)
        {
            if (dbContext.Entry(item).State == EntityState.Detached)
                dbContext.WarehouseOrderItems.Add(item);
        }
        foreach (var assignee in warehouseOrder.Assignees)
        {
            if (dbContext.Entry(assignee).State == EntityState.Detached)
                dbContext.WarehouseOrderAssignees.Add(assignee);
        }

        dbContext.Entry(warehouseOrder).Property(x => x.Version).OriginalValue = expectedVersion;
        dbContext.AuditEntries.Add(audit);
        try { await dbContext.SaveChangesAsync(ct); return WarehouseOrderStoreResult.Success; }
        catch (DbUpdateConcurrencyException) { return WarehouseOrderStoreResult.Conflict; }
    }

    public async Task<WarehouseOrderStoreResult> DeleteAsync(WarehouseOrder warehouseOrder, long expectedVersion, AuditEntry audit, CancellationToken ct)
    {
        dbContext.Entry(warehouseOrder).Property(x => x.Version).OriginalValue = expectedVersion;
        dbContext.WarehouseOrders.Remove(warehouseOrder);
        dbContext.AuditEntries.Add(audit);
        try { await dbContext.SaveChangesAsync(ct); return WarehouseOrderStoreResult.Success; }
        catch (DbUpdateConcurrencyException) { return WarehouseOrderStoreResult.Conflict; }
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
