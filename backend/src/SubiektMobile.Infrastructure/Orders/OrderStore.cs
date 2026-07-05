using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubiektMobile.Application.Orders;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;
using SubiektMobile.Infrastructure.Persistence.Application;

namespace SubiektMobile.Infrastructure.Orders;

public sealed class OrderStore(ApplicationDbContext dbContext) : IOrderStore
{
    public async Task<PagedResult<OrderListItemDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = dbContext.Orders.AsNoTracking();
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Number)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new OrderListItemDto(x.Id, x.Number, x.CustomerName, x.DueDate, x.Status,
                x.Items.Count, x.UpdatedAtUtc, x.Version)).ToListAsync(ct);
        return new PagedResult<OrderListItemDto>(items, page, pageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize));
    }

    public Task<Order?> FindAsync(Guid id, bool tracking, CancellationToken ct)
    {
        IQueryable<Order> query = dbContext.Orders.Include(x => x.Items).Include(x => x.Assignees);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<OrderStoreResult> AddAsync(Order order, AuditEntry audit, CancellationToken ct)
    {
        dbContext.Orders.Add(order);
        dbContext.AuditEntries.Add(audit);
        try { await dbContext.SaveChangesAsync(ct); return OrderStoreResult.Success; }
        catch (DbUpdateException ex) when (FindPostgres(ex)?.SqlState == PostgresErrorCodes.UniqueViolation)
        { return OrderStoreResult.Conflict; }
    }

    public async Task<OrderStoreResult> SaveAsync(Order order, long expectedVersion, AuditEntry audit, CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            if (dbContext.Entry(item).State == EntityState.Detached)
                dbContext.OrderItems.Add(item);
        }
        foreach (var assignee in order.Assignees)
        {
            if (dbContext.Entry(assignee).State == EntityState.Detached)
                dbContext.OrderAssignees.Add(assignee);
        }

        dbContext.Entry(order).Property(x => x.Version).OriginalValue = expectedVersion;
        dbContext.AuditEntries.Add(audit);
        try { await dbContext.SaveChangesAsync(ct); return OrderStoreResult.Success; }
        catch (DbUpdateConcurrencyException) { return OrderStoreResult.Conflict; }
    }

    public async Task<OrderStoreResult> DeleteAsync(Order order, long expectedVersion, AuditEntry audit, CancellationToken ct)
    {
        dbContext.Entry(order).Property(x => x.Version).OriginalValue = expectedVersion;
        dbContext.Orders.Remove(order);
        dbContext.AuditEntries.Add(audit);
        try { await dbContext.SaveChangesAsync(ct); return OrderStoreResult.Success; }
        catch (DbUpdateConcurrencyException) { return OrderStoreResult.Conflict; }
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
