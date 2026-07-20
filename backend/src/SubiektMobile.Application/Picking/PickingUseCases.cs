using MediatR;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Application.Picking;

public sealed record ListPickingWarehouseOrdersQuery(int Page, int PageSize, string? Search,
    PickingWarehouseOrderStatus? Status, DateOnly? DueDateFrom, DateOnly? DueDateTo, string? Customer)
    : IRequest<PagedResult<PickingWarehouseOrderListItemDto>>;
public sealed record GetPickingWarehouseOrderQuery(Guid WarehouseOrderId) : IRequest<PickingWarehouseOrderDetailsDto>;
public sealed record ListPickingHistoryQuery(Guid WarehouseOrderId, int Page, int PageSize)
    : IRequest<PagedResult<PickingHistoryItemDto>>;
public sealed record ReservePickingItemCommand(Guid WarehouseOrderId, Guid ItemId, Guid OperationId, long ItemVersion)
    : IRequest<PickingWarehouseOrderDetailsDto>;
public sealed record ReleasePickingItemCommand(Guid WarehouseOrderId, Guid ItemId, Guid OperationId, long ItemVersion)
    : IRequest<PickingWarehouseOrderDetailsDto>;
public sealed record PackPickingItemCommand(Guid WarehouseOrderId, Guid ItemId, Guid OperationId, long ItemVersion,
    decimal PackedQuantity) : IRequest<PickingWarehouseOrderDetailsDto>;
public sealed record UndoPackedPickingItemCommand(Guid WarehouseOrderId, Guid ItemId, Guid OperationId, long ItemVersion)
    : IRequest<PickingWarehouseOrderDetailsDto>;

public sealed class ListPickingWarehouseOrdersHandler(IPickingStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<ListPickingWarehouseOrdersQuery, PagedResult<PickingWarehouseOrderListItemDto>>
{
    public Task<PagedResult<PickingWarehouseOrderListItemDto>> Handle(ListPickingWarehouseOrdersQuery request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersReadPublished);
        ValidatePage(request.Page, request.PageSize);
        if (request.DueDateFrom > request.DueDateTo)
            throw new RequestValidationException("dueDateFrom cannot be later than dueDateTo.");
        return store.ListAsync(new(request.Page, request.PageSize, Clean(request.Search), request.Status,
            request.DueDateFrom, request.DueDateTo, Clean(request.Customer)), actor, ct);
    }

    internal static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new RequestValidationException("Page must be at least 1 and pageSize must be between 1 and 100.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class GetPickingWarehouseOrderHandler(IPickingStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<GetPickingWarehouseOrderQuery, PickingWarehouseOrderDetailsDto>
{
    public async Task<PickingWarehouseOrderDetailsDto> Handle(GetPickingWarehouseOrderQuery request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersReadPublished);
        var warehouseOrder = await FindPublished(request.WarehouseOrderId, store, false, ct);
        var assignments = await store.ListPalletAssignmentsAsync(warehouseOrder.Id, ct);
        return Map(warehouseOrder, actor, assignments);
    }

    internal static async Task<WarehouseOrder> FindPublished(Guid warehouseOrderId, IPickingStore store, bool tracking,
        CancellationToken ct)
    {
        var warehouseOrder = await store.FindWarehouseOrderAsync(warehouseOrderId, tracking, ct)
            ?? throw new ResourceNotFoundException("Picking warehouse order was not found.");
        if (warehouseOrder.Status != WarehouseOrderStatus.ReadyForPicking)
            throw new ResourceNotFoundException("Picking warehouse order was not found.");
        return warehouseOrder;
    }

    internal static PickingWarehouseOrderDetailsDto Map(WarehouseOrder warehouseOrder, CurrentActor actor,
        IReadOnlyDictionary<Guid, IReadOnlyList<PickingPalletAssignmentDto>>? palletAssignments = null)
    {
        var assigned = IsAssigned(warehouseOrder, actor);
        var canExecute = actor.Kind == ActorKind.Administrator || assigned;
        var completed = warehouseOrder.Items.Count(IsCompleted);
        var assignmentsByItem = palletAssignments ?? new Dictionary<Guid, IReadOnlyList<PickingPalletAssignmentDto>>();
        var canCreatePallet = canExecute && actor.Permissions.Contains(Permissions.PalletsManage) &&
            warehouseOrder.Items.Any(item => AvailableForPallet(item, assignmentsByItem.GetValueOrDefault(item.Id)) > 0);
        return new(warehouseOrder.Id, warehouseOrder.Number, warehouseOrder.CustomerName, warehouseOrder.DueDate, warehouseOrder.PickingMode,
            Status(warehouseOrder.Items), warehouseOrder.Items.Count, completed, Progress(completed, warehouseOrder.Items.Count),
            assigned, canExecute, canCreatePallet, warehouseOrder.Items.OrderBy(x => x.ProductName).Select(item =>
            {
                var itemAssignments = assignmentsByItem.GetValueOrDefault(item.Id) ?? [];
                var palletizedQuantity = itemAssignments.Sum(x => x.Quantity);
                var availableForPallet = AvailableForPallet(item, itemAssignments);
                var isAdmin = actor.Kind == ActorKind.Administrator;
                var ownsReservation = item.ReservedById == actor.Id;
                var ownsPacked = item.PackedById == actor.Id;
                var canReserve = canExecute && warehouseOrder.PickingMode == PickingMode.SharedTeam &&
                    item.Status == WarehouseOrderItemStatus.ToPick;
                var canRelease = canExecute && item.Status == WarehouseOrderItemStatus.Picking &&
                    (isAdmin || ownsReservation);
                var canPack = canExecute && (warehouseOrder.PickingMode == PickingMode.SingleAssignee
                    ? item.Status == WarehouseOrderItemStatus.ToPick
                    : item.Status == WarehouseOrderItemStatus.Picking && (isAdmin || ownsReservation));
                var canUndo = canExecute && item.Status != WarehouseOrderItemStatus.AssignedToPallet &&
                    item.PackedQuantity > 0 && palletizedQuantity == 0 &&
                    (isAdmin || ownsPacked);
                return new PickingItemDto(item.Id, item.ProductId, item.ProductName, item.ProductSymbol,
                    item.Quantity, item.Quantity - (item.PackedQuantity ?? 0), item.Unit, item.Status, item.Version,
                    Actor(item.ReservedByKind, item.ReservedById, item.ReservedByName, item.ReservedAtUtc),
                    item.PackedQuantity,
                    Actor(item.PackedByKind, item.PackedById, item.PackedByName, item.PackedAtUtc),
                    palletizedQuantity, availableForPallet, itemAssignments,
                    new(canReserve, canRelease, canPack, canUndo));
            }).ToList());
    }

    internal static bool IsAssigned(WarehouseOrder warehouseOrder, CurrentActor actor) =>
        actor.Kind == ActorKind.Employee && warehouseOrder.Assignees.Any(x => x.EmployeeId == actor.Id);
    internal static bool IsCompleted(WarehouseOrderItem item) =>
        item.Status is WarehouseOrderItemStatus.Packed or WarehouseOrderItemStatus.AssignedToPallet;
    internal static PickingWarehouseOrderStatus Status(IEnumerable<WarehouseOrderItem> items)
    {
        var list = items.ToList();
        if (list.Count > 0 && list.All(IsCompleted)) return PickingWarehouseOrderStatus.Completed;
        if (list.All(x => x.Status == WarehouseOrderItemStatus.ToPick && (x.PackedQuantity ?? 0) == 0))
            return PickingWarehouseOrderStatus.Waiting;
        return PickingWarehouseOrderStatus.InProgress;
    }
    internal static int Progress(int completed, int total) => total == 0 ? 0 : (int)Math.Round(completed * 100m / total);

    private static decimal AvailableForPallet(WarehouseOrderItem item,
        IReadOnlyCollection<PickingPalletAssignmentDto>? assignments)
    {
        var palletized = assignments?.Sum(x => x.Quantity) ?? 0;
        return Math.Max(0, (item.PackedQuantity ?? 0) - palletized);
    }

    private static PickingActorDto? Actor(ActorKind? kind, Guid? id, string? name, DateTimeOffset? at) =>
        kind.HasValue && id.HasValue && name is not null && at.HasValue
            ? new(kind.Value, id.Value, name, at.Value) : null;
}

public sealed class ListPickingHistoryHandler(IPickingStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<ListPickingHistoryQuery, PagedResult<PickingHistoryItemDto>>
{
    public async Task<PagedResult<PickingHistoryItemDto>> Handle(ListPickingHistoryQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.WarehouseOrdersReadPublished);
        ListPickingWarehouseOrdersHandler.ValidatePage(request.Page, request.PageSize);
        await GetPickingWarehouseOrderHandler.FindPublished(request.WarehouseOrderId, store, false, ct);
        return await store.ListHistoryAsync(request.WarehouseOrderId, request.Page, request.PageSize, ct);
    }
}

public abstract class PickingMutationHandlerBase(IPickingStore store,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
{
    protected async Task<PickingWarehouseOrderDetailsDto> Execute(Guid warehouseOrderId, Guid itemId, Guid operationId,
        long itemVersion, PickingAction action, decimal? packedQuantity, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.PickingExecute);
        if (operationId == Guid.Empty || itemVersion <= 0)
            throw new RequestValidationException("operationId and a positive itemVersion are required.");

        var previous = await store.FindOperationAsync(operationId, ct);
        if (previous is not null)
        {
            EnsureSameOperation(previous, warehouseOrderId, itemId, action, packedQuantity, actor);
            return await LoadMapped(warehouseOrderId, actor, ct);
        }

        var warehouseOrder = await GetPickingWarehouseOrderHandler.FindPublished(warehouseOrderId, store, true, ct);
        EnsureCanExecute(warehouseOrder, actor);
        var item = warehouseOrder.Items.SingleOrDefault(x => x.Id == itemId)
            ?? throw new ResourceNotFoundException("Warehouse order item was not found.");
        if (item.Version != itemVersion)
            throw new ResourceConflictException("The item was modified by another request.");

        var fromStatus = item.Status;
        var pickingActor = new PickingActor(actor.Kind, actor.Id, actor.DisplayName);
        var now = time.GetUtcNow();
        try
        {
            switch (action)
            {
                case PickingAction.Reserved:
                    warehouseOrder.ReserveItem(itemId, pickingActor, now);
                    break;
                case PickingAction.Released:
                    warehouseOrder.ReleaseItem(itemId, pickingActor, actor.Kind == ActorKind.Administrator, now);
                    break;
                case PickingAction.Packed:
                    warehouseOrder.PackItem(itemId, packedQuantity!.Value, pickingActor,
                        actor.Kind == ActorKind.Administrator, now);
                    break;
                case PickingAction.PackingUndone:
                    warehouseOrder.UndoPackedItem(itemId, pickingActor, actor.Kind == ActorKind.Administrator, now,
                        await store.GetPalletizedQuantityAsync(itemId, ct));
                    break;
                default:
                    throw new RequestValidationException("Unsupported picking action.");
            }
        }
        catch (KeyNotFoundException) { throw new ResourceNotFoundException("Warehouse order item was not found."); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { throw new RequestValidationException(ex.Message); }

        var pickingEvent = WarehouseOrderPickingEvent.Create(operationId, warehouseOrder, item, action, fromStatus,
            action == PickingAction.Packed ? packedQuantity : null, pickingActor, now);
        var result = await store.SaveMutationAsync(item, itemVersion, pickingEvent,
            audits.Create(actor, action.ToString(), "WarehouseOrderItem", item.Id, now), ct);
        if (result == PickingStoreMutationResult.Conflict)
            throw new ResourceConflictException("The item was modified by another request.");
        if (result == PickingStoreMutationResult.DuplicateOperation)
        {
            var duplicated = await store.FindOperationAsync(operationId, ct)
                ?? throw new ResourceConflictException("The operation could not be completed.");
            EnsureSameOperation(duplicated, warehouseOrderId, itemId, action, packedQuantity, actor);
            warehouseOrder = await GetPickingWarehouseOrderHandler.FindPublished(warehouseOrderId, store, false, ct);
        }
        var assignments = await store.ListPalletAssignmentsAsync(warehouseOrderId, ct);
        return GetPickingWarehouseOrderHandler.Map(warehouseOrder, actor, assignments);
    }

    private async Task<PickingWarehouseOrderDetailsDto> LoadMapped(Guid warehouseOrderId, CurrentActor actor, CancellationToken ct)
    {
        var warehouseOrder = await GetPickingWarehouseOrderHandler.FindPublished(warehouseOrderId, store, false, ct);
        var assignments = await store.ListPalletAssignmentsAsync(warehouseOrderId, ct);
        return GetPickingWarehouseOrderHandler.Map(warehouseOrder, actor, assignments);
    }

    private static void EnsureCanExecute(WarehouseOrder warehouseOrder, CurrentActor actor)
    {
        if (actor.Kind != ActorKind.Administrator && !GetPickingWarehouseOrderHandler.IsAssigned(warehouseOrder, actor))
            throw new AccessDeniedException("This employee is not assigned to the order.");
    }

    private static void EnsureSameOperation(WarehouseOrderPickingEvent entry, Guid warehouseOrderId, Guid itemId,
        PickingAction action, decimal? packedQuantity, CurrentActor actor)
    {
        if (entry.WarehouseOrderId != warehouseOrderId || entry.WarehouseOrderItemId != itemId || entry.Action != action ||
            entry.ActorKind != actor.Kind || entry.ActorId != actor.Id ||
            (action == PickingAction.Packed && entry.PackedQuantity != packedQuantity))
            throw new ResourceConflictException("operationId has already been used for another operation.");
    }
}

public sealed class ReservePickingItemHandler(IPickingStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : PickingMutationHandlerBase(store, authorization, audits, time),
    IRequestHandler<ReservePickingItemCommand, PickingWarehouseOrderDetailsDto>
{
    public Task<PickingWarehouseOrderDetailsDto> Handle(ReservePickingItemCommand request, CancellationToken ct) =>
        Execute(request.WarehouseOrderId, request.ItemId, request.OperationId, request.ItemVersion,
            PickingAction.Reserved, null, ct);
}

public sealed class ReleasePickingItemHandler(IPickingStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : PickingMutationHandlerBase(store, authorization, audits, time),
    IRequestHandler<ReleasePickingItemCommand, PickingWarehouseOrderDetailsDto>
{
    public Task<PickingWarehouseOrderDetailsDto> Handle(ReleasePickingItemCommand request, CancellationToken ct) =>
        Execute(request.WarehouseOrderId, request.ItemId, request.OperationId, request.ItemVersion,
            PickingAction.Released, null, ct);
}

public sealed class PackPickingItemHandler(IPickingStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : PickingMutationHandlerBase(store, authorization, audits, time),
    IRequestHandler<PackPickingItemCommand, PickingWarehouseOrderDetailsDto>
{
    public Task<PickingWarehouseOrderDetailsDto> Handle(PackPickingItemCommand request, CancellationToken ct) =>
        Execute(request.WarehouseOrderId, request.ItemId, request.OperationId, request.ItemVersion,
            PickingAction.Packed, request.PackedQuantity, ct);
}

public sealed class UndoPackedPickingItemHandler(IPickingStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : PickingMutationHandlerBase(store, authorization, audits, time),
    IRequestHandler<UndoPackedPickingItemCommand, PickingWarehouseOrderDetailsDto>
{
    public Task<PickingWarehouseOrderDetailsDto> Handle(UndoPackedPickingItemCommand request, CancellationToken ct) =>
        Execute(request.WarehouseOrderId, request.ItemId, request.OperationId, request.ItemVersion,
            PickingAction.PackingUndone, null, ct);
}
