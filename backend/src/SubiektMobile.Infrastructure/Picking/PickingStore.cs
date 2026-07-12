using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubiektMobile.Application.Picking;
using SubiektMobile.Application.Products;
using SubiektMobile.Application.Identity;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;
using SubiektMobile.Infrastructure.Persistence.Application;

namespace SubiektMobile.Infrastructure.Picking;

public sealed class PickingStore(ApplicationDbContext dbContext) : IPickingStore
{
    public async Task<PagedResult<PickingOrderListItemDto>> ListAsync(PickingOrderListFilter filter,
        CurrentActor actor, CancellationToken ct)
    {
        var query = dbContext.Orders.AsNoTracking().Where(x => x.Status == OrderStatus.ReadyForPicking);
        if (filter.Search is not null)
        {
            var pattern = $"%{filter.Search}%";
            query = query.Where(x => EF.Functions.ILike(x.Number, pattern) ||
                EF.Functions.ILike(x.CustomerName, pattern));
        }
        if (filter.Customer is not null)
            query = query.Where(x => EF.Functions.ILike(x.CustomerName, $"%{filter.Customer}%"));
        if (filter.DueDateFrom.HasValue) query = query.Where(x => x.DueDate >= filter.DueDateFrom.Value);
        if (filter.DueDateTo.HasValue) query = query.Where(x => x.DueDate <= filter.DueDateTo.Value);
        if (filter.Status.HasValue)
        {
            query = filter.Status.Value switch
            {
                PickingOrderStatus.Waiting => query.Where(x =>
                    !x.Items.Any(item => item.Status != OrderItemStatus.ToPick || item.PackedQuantity > 0)),
                PickingOrderStatus.Completed => query.Where(x =>
                    !x.Items.Any(item => item.Status != OrderItemStatus.Packed &&
                        item.Status != OrderItemStatus.AssignedToPallet)),
                PickingOrderStatus.InProgress => query.Where(x =>
                    x.Items.Any(item => item.Status != OrderItemStatus.ToPick || item.PackedQuantity > 0) &&
                    x.Items.Any(item => item.Status != OrderItemStatus.Packed &&
                        item.Status != OrderItemStatus.AssignedToPallet)),
                _ => query
            };
        }

        var count = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.DueDate).ThenBy(x => x.Number)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(x => new
            {
                x.Id, x.Number, x.CustomerName, x.DueDate, x.PickingMode,
                Total = x.Items.Count,
                Completed = x.Items.Count(item => item.Status == OrderItemStatus.Packed ||
                    item.Status == OrderItemStatus.AssignedToPallet),
                Started = x.Items.Count(item => item.Status != OrderItemStatus.ToPick || item.PackedQuantity > 0),
                Assigned = actor.Kind == ActorKind.Employee && x.Assignees.Any(a => a.EmployeeId == actor.Id)
            }).ToListAsync(ct);
        var items = rows.Select(x => new PickingOrderListItemDto(x.Id, x.Number, x.CustomerName,
            x.DueDate, x.PickingMode,
            x.Completed == x.Total && x.Total > 0 ? PickingOrderStatus.Completed
                : x.Started == 0 ? PickingOrderStatus.Waiting : PickingOrderStatus.InProgress,
            x.Total, x.Completed, x.Total == 0 ? 0 : (int)Math.Round(x.Completed * 100m / x.Total),
            x.Assigned)).ToList();
        return new(items, filter.Page, filter.PageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)filter.PageSize));
    }

    public Task<Order?> FindOrderAsync(Guid orderId, bool tracking, CancellationToken ct)
    {
        IQueryable<Order> query = dbContext.Orders.Include(x => x.Items).Include(x => x.Assignees);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == orderId && x.Status == OrderStatus.ReadyForPicking, ct);
    }

    public async Task<PagedResult<PickingHistoryItemDto>> ListHistoryAsync(Guid orderId, int page,
        int pageSize, CancellationToken ct)
    {
        var query = dbContext.OrderPickingEvents.AsNoTracking().Where(x => x.OrderId == orderId);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PickingHistoryItemDto(x.Id, x.OperationId, x.OrderItemId, x.ProductName,
                x.Action, x.FromStatus, x.ToStatus, x.PackedQuantity, x.ActorKind, x.ActorId,
                x.ActorDisplayName, x.OccurredAtUtc)).ToListAsync(ct);
        return new(items, page, pageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize));
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PickingPalletAssignmentDto>>> ListPalletAssignmentsAsync(
        Guid orderId, CancellationToken ct)
    {
        var rows = await (
            from palletItem in dbContext.PalletItems.AsNoTracking()
            join pallet in dbContext.Pallets.AsNoTracking() on palletItem.PalletId equals pallet.Id
            where pallet.OrderId == orderId
            orderby pallet.Number
            select new
            {
                palletItem.OrderItemId,
                Assignment = new PickingPalletAssignmentDto(pallet.Id, pallet.Number, palletItem.Quantity)
            }).ToListAsync(ct);
        return rows.GroupBy(x => x.OrderItemId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<PickingPalletAssignmentDto>)x.Select(row => row.Assignment).ToList());
    }

    public Task<decimal> GetPalletizedQuantityAsync(Guid orderItemId, CancellationToken ct) =>
        dbContext.PalletItems.AsNoTracking()
            .Where(x => x.OrderItemId == orderItemId)
            .SumAsync(x => x.Quantity, ct);

    public Task<OrderPickingEvent?> FindOperationAsync(Guid operationId, CancellationToken ct) =>
        dbContext.OrderPickingEvents.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == operationId, ct);

    public async Task<PickingStoreMutationResult> SaveMutationAsync(OrderItem item, long expectedVersion,
        OrderPickingEvent pickingEvent, AuditEntry audit, CancellationToken ct)
    {
        dbContext.Entry(item).Property(x => x.Version).OriginalValue = expectedVersion;
        dbContext.OrderPickingEvents.Add(pickingEvent);
        dbContext.AuditEntries.Add(audit);
        try
        {
            await dbContext.SaveChangesAsync(ct);
            return PickingStoreMutationResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return PickingStoreMutationResult.Conflict;
        }
        catch (DbUpdateException ex) when (FindPostgres(ex)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            dbContext.ChangeTracker.Clear();
            return PickingStoreMutationResult.DuplicateOperation;
        }
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
