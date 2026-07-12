using Microsoft.EntityFrameworkCore;
using Npgsql;
using SubiektMobile.Application.Pallets;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;
using SubiektMobile.Infrastructure.Persistence.Application;

namespace SubiektMobile.Infrastructure.Pallets;

public sealed class PalletStore(ApplicationDbContext dbContext) : IPalletStore
{
    public async Task<PagedResult<PalletListItemDto>> ListAsync(int page, int pageSize,
        CancellationToken ct)
    {
        var query =
            from pallet in dbContext.Pallets.AsNoTracking()
            join order in dbContext.Orders.AsNoTracking() on pallet.OrderId equals order.Id
            orderby pallet.ClosedAtUtc descending, pallet.Number descending
            select new PalletListItemDto(pallet.Id, pallet.OrderId, order.Number,
                pallet.Number, order.CustomerName, pallet.Status, pallet.GoodsWeightKg,
                pallet.EmptyPalletWeightKg, pallet.TotalWeightKg, pallet.Items.Count,
                pallet.ClosedAtUtc);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<PalletListItemDto>(items, page, pageSize, total, totalPages);
    }

    public Task<Order?> FindOrderAsync(Guid orderId, bool tracking, CancellationToken ct)
    {
        IQueryable<Order> query = dbContext.Orders.Include(x => x.Items).Include(x => x.Assignees);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == orderId, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetPalletizedQuantitiesAsync(
        Guid orderId, CancellationToken ct)
    {
        var rows = await dbContext.PalletItems.AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .GroupBy(x => x.OrderItemId)
            .Select(x => new { OrderItemId = x.Key, Quantity = x.Sum(item => item.Quantity) })
            .ToListAsync(ct);
        return rows.ToDictionary(x => x.OrderItemId, x => x.Quantity);
    }

    public Task<PalletOperationSnapshot?> FindOperationAsync(Guid operationId, CancellationToken ct) =>
        dbContext.Pallets.AsNoTracking()
            .Where(x => x.OperationId == operationId)
            .Select(x => new PalletOperationSnapshot(x.Id, x.OperationId, x.OrderId,
                x.EmptyPalletWeightKg,
                x.Items.OrderBy(item => item.OrderItemId)
                    .Select(item => new PalletOperationItemSnapshot(item.OrderItemId, item.Quantity))
                    .ToList()))
            .SingleOrDefaultAsync(ct);

    public async Task<PalletDetailsDto?> GetDetailsAsync(Guid palletId, CancellationToken ct)
    {
        var header = await (
            from pallet in dbContext.Pallets.AsNoTracking()
            join order in dbContext.Orders.AsNoTracking() on pallet.OrderId equals order.Id
            where pallet.Id == palletId
            select new
            {
                Pallet = pallet,
                OrderNumber = order.Number,
                order.CustomerName
            }).SingleOrDefaultAsync(ct);
        if (header is null) return null;

        var items = await (
            from palletItem in dbContext.PalletItems.AsNoTracking()
            join orderItem in dbContext.OrderItems.AsNoTracking()
                on palletItem.OrderItemId equals orderItem.Id
            where palletItem.PalletId == palletId
            orderby orderItem.ProductName
            select new PalletDetailsItemDto(orderItem.Id, orderItem.ProductId,
                orderItem.ProductName, orderItem.ProductSymbol, palletItem.Quantity,
                orderItem.Unit, palletItem.UnitWeightKg, palletItem.LineWeightKg))
            .ToListAsync(ct);

        var label = new PalletLabelPreviewDto(header.OrderNumber, header.Pallet.Number,
            header.CustomerName, header.Pallet.GoodsWeightKg, header.Pallet.EmptyPalletWeightKg,
            header.Pallet.TotalWeightKg,
            items.Select(x => new PalletLabelItemDto(x.ProductName, x.Quantity, x.Unit)).ToList());

        var issues = await dbContext.AuditEntries.AsNoTracking()
            .Where(x => x.TargetType == "Pallet" && x.TargetId == palletId &&
                (x.Action == "PalletLabelPrintIssued" || x.Action == "PalletLabelDownloadIssued"))
            .OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Action,
                x.ActorKind,
                x.ActorDisplayName,
                x.OccurredAtUtc
            })
            .ToListAsync(ct);
        var labelIssues = issues.Select((issue, index) => new PalletLabelIssueDto(index + 1,
            issue.Action == "PalletLabelPrintIssued"
                ? PalletLabelIssueMode.Print
                : PalletLabelIssueMode.Download,
            issue.ActorKind, issue.ActorDisplayName, issue.OccurredAtUtc)).ToList();

        return new(header.Pallet.Id, header.Pallet.OrderId, header.OrderNumber,
            header.Pallet.Number, header.CustomerName, header.Pallet.Status,
            header.Pallet.EmptyPalletWeightKg, header.Pallet.GoodsWeightKg,
            header.Pallet.TotalWeightKg, header.Pallet.ClosedByKind,
            header.Pallet.ClosedById, header.Pallet.ClosedByName,
            header.Pallet.ClosedAtUtc, items, label, labelIssues);
    }

    public async Task SaveLabelIssueAsync(AuditEntry audit, CancellationToken ct)
    {
        dbContext.AuditEntries.Add(audit);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<PalletStoreMutationResult> SaveClosedAsync(Pallet pallet,
        IReadOnlyCollection<PalletItemVersion> expectedItemVersions, AuditEntry audit, CancellationToken ct)
    {
        foreach (var itemVersion in expectedItemVersions)
        {
            var entry = dbContext.ChangeTracker.Entries<OrderItem>()
                .SingleOrDefault(x => x.Entity.Id == itemVersion.OrderItemId);
            if (entry is null) return PalletStoreMutationResult.Conflict;
            entry.Property(x => x.Version).OriginalValue = itemVersion.Version;
        }

        dbContext.Pallets.Add(pallet);
        dbContext.AuditEntries.Add(audit);
        try
        {
            await dbContext.SaveChangesAsync(ct);
            return PalletStoreMutationResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return PalletStoreMutationResult.Conflict;
        }
        catch (DbUpdateException ex) when (FindPostgres(ex) is { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            dbContext.ChangeTracker.Clear();
            return postgres.ConstraintName == "IX_pallets_operation_id"
                ? PalletStoreMutationResult.DuplicateOperation
                : PalletStoreMutationResult.Conflict;
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
