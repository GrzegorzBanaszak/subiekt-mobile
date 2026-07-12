using MediatR;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;

namespace SubiektMobile.Application.Picking;

public sealed record ListPickingOrdersQuery(int Page, int PageSize, string? Search,
    PickingOrderStatus? Status, DateOnly? DueDateFrom, DateOnly? DueDateTo, string? Customer)
    : IRequest<PagedResult<PickingOrderListItemDto>>;
public sealed record GetPickingOrderQuery(Guid OrderId) : IRequest<PickingOrderDetailsDto>;
public sealed record ListPickingHistoryQuery(Guid OrderId, int Page, int PageSize)
    : IRequest<PagedResult<PickingHistoryItemDto>>;
public sealed record ReservePickingItemCommand(Guid OrderId, Guid ItemId, Guid OperationId, long ItemVersion)
    : IRequest<PickingOrderDetailsDto>;
public sealed record ReleasePickingItemCommand(Guid OrderId, Guid ItemId, Guid OperationId, long ItemVersion)
    : IRequest<PickingOrderDetailsDto>;
public sealed record PackPickingItemCommand(Guid OrderId, Guid ItemId, Guid OperationId, long ItemVersion,
    decimal PackedQuantity) : IRequest<PickingOrderDetailsDto>;
public sealed record UndoPackedPickingItemCommand(Guid OrderId, Guid ItemId, Guid OperationId, long ItemVersion)
    : IRequest<PickingOrderDetailsDto>;

public sealed class ListPickingOrdersHandler(IPickingStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<ListPickingOrdersQuery, PagedResult<PickingOrderListItemDto>>
{
    public Task<PagedResult<PickingOrderListItemDto>> Handle(ListPickingOrdersQuery request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.OrdersReadPublished);
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

public sealed class GetPickingOrderHandler(IPickingStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<GetPickingOrderQuery, PickingOrderDetailsDto>
{
    public async Task<PickingOrderDetailsDto> Handle(GetPickingOrderQuery request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.OrdersReadPublished);
        var order = await FindPublished(request.OrderId, store, false, ct);
        var assignments = await store.ListPalletAssignmentsAsync(order.Id, ct);
        return Map(order, actor, assignments);
    }

    internal static async Task<Order> FindPublished(Guid orderId, IPickingStore store, bool tracking,
        CancellationToken ct)
    {
        var order = await store.FindOrderAsync(orderId, tracking, ct)
            ?? throw new ResourceNotFoundException("Picking order was not found.");
        if (order.Status != OrderStatus.ReadyForPicking)
            throw new ResourceNotFoundException("Picking order was not found.");
        return order;
    }

    internal static PickingOrderDetailsDto Map(Order order, CurrentActor actor,
        IReadOnlyDictionary<Guid, IReadOnlyList<PickingPalletAssignmentDto>>? palletAssignments = null)
    {
        var assigned = IsAssigned(order, actor);
        var canExecute = actor.Kind == ActorKind.Administrator || assigned;
        var completed = order.Items.Count(IsCompleted);
        var assignmentsByItem = palletAssignments ?? new Dictionary<Guid, IReadOnlyList<PickingPalletAssignmentDto>>();
        var canCreatePallet = canExecute && actor.Permissions.Contains(Permissions.PalletsManage) &&
            order.Items.Any(item => AvailableForPallet(item, assignmentsByItem.GetValueOrDefault(item.Id)) > 0);
        return new(order.Id, order.Number, order.CustomerName, order.DueDate, order.PickingMode,
            Status(order.Items), order.Items.Count, completed, Progress(completed, order.Items.Count),
            assigned, canExecute, canCreatePallet, order.Items.OrderBy(x => x.ProductName).Select(item =>
            {
                var itemAssignments = assignmentsByItem.GetValueOrDefault(item.Id) ?? [];
                var palletizedQuantity = itemAssignments.Sum(x => x.Quantity);
                var availableForPallet = AvailableForPallet(item, itemAssignments);
                var isAdmin = actor.Kind == ActorKind.Administrator;
                var ownsReservation = item.ReservedById == actor.Id;
                var ownsPacked = item.PackedById == actor.Id;
                var canReserve = canExecute && order.PickingMode == PickingMode.SharedTeam &&
                    item.Status == OrderItemStatus.ToPick;
                var canRelease = canExecute && item.Status == OrderItemStatus.Picking &&
                    (isAdmin || ownsReservation);
                var canPack = canExecute && (order.PickingMode == PickingMode.SingleAssignee
                    ? item.Status == OrderItemStatus.ToPick
                    : item.Status == OrderItemStatus.Picking && (isAdmin || ownsReservation));
                var canUndo = canExecute && item.Status != OrderItemStatus.AssignedToPallet &&
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

    internal static bool IsAssigned(Order order, CurrentActor actor) =>
        actor.Kind == ActorKind.Employee && order.Assignees.Any(x => x.EmployeeId == actor.Id);
    internal static bool IsCompleted(OrderItem item) =>
        item.Status is OrderItemStatus.Packed or OrderItemStatus.AssignedToPallet;
    internal static PickingOrderStatus Status(IEnumerable<OrderItem> items)
    {
        var list = items.ToList();
        if (list.Count > 0 && list.All(IsCompleted)) return PickingOrderStatus.Completed;
        if (list.All(x => x.Status == OrderItemStatus.ToPick && (x.PackedQuantity ?? 0) == 0))
            return PickingOrderStatus.Waiting;
        return PickingOrderStatus.InProgress;
    }
    internal static int Progress(int completed, int total) => total == 0 ? 0 : (int)Math.Round(completed * 100m / total);

    private static decimal AvailableForPallet(OrderItem item,
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
        authorization.Require(Permissions.OrdersReadPublished);
        ListPickingOrdersHandler.ValidatePage(request.Page, request.PageSize);
        await GetPickingOrderHandler.FindPublished(request.OrderId, store, false, ct);
        return await store.ListHistoryAsync(request.OrderId, request.Page, request.PageSize, ct);
    }
}

public abstract class PickingMutationHandlerBase(IPickingStore store,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
{
    protected async Task<PickingOrderDetailsDto> Execute(Guid orderId, Guid itemId, Guid operationId,
        long itemVersion, PickingAction action, decimal? packedQuantity, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.PickingExecute);
        if (operationId == Guid.Empty || itemVersion <= 0)
            throw new RequestValidationException("operationId and a positive itemVersion are required.");

        var previous = await store.FindOperationAsync(operationId, ct);
        if (previous is not null)
        {
            EnsureSameOperation(previous, orderId, itemId, action, packedQuantity, actor);
            return await LoadMapped(orderId, actor, ct);
        }

        var order = await GetPickingOrderHandler.FindPublished(orderId, store, true, ct);
        EnsureCanExecute(order, actor);
        var item = order.Items.SingleOrDefault(x => x.Id == itemId)
            ?? throw new ResourceNotFoundException("Order item was not found.");
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
                    order.ReserveItem(itemId, pickingActor, now);
                    break;
                case PickingAction.Released:
                    order.ReleaseItem(itemId, pickingActor, actor.Kind == ActorKind.Administrator, now);
                    break;
                case PickingAction.Packed:
                    order.PackItem(itemId, packedQuantity!.Value, pickingActor,
                        actor.Kind == ActorKind.Administrator, now);
                    break;
                case PickingAction.PackingUndone:
                    order.UndoPackedItem(itemId, pickingActor, actor.Kind == ActorKind.Administrator, now,
                        await store.GetPalletizedQuantityAsync(itemId, ct));
                    break;
                default:
                    throw new RequestValidationException("Unsupported picking action.");
            }
        }
        catch (KeyNotFoundException) { throw new ResourceNotFoundException("Order item was not found."); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { throw new RequestValidationException(ex.Message); }

        var pickingEvent = OrderPickingEvent.Create(operationId, order, item, action, fromStatus,
            action == PickingAction.Packed ? packedQuantity : null, pickingActor, now);
        var result = await store.SaveMutationAsync(item, itemVersion, pickingEvent,
            audits.Create(actor, action.ToString(), "OrderItem", item.Id, now), ct);
        if (result == PickingStoreMutationResult.Conflict)
            throw new ResourceConflictException("The item was modified by another request.");
        if (result == PickingStoreMutationResult.DuplicateOperation)
        {
            var duplicated = await store.FindOperationAsync(operationId, ct)
                ?? throw new ResourceConflictException("The operation could not be completed.");
            EnsureSameOperation(duplicated, orderId, itemId, action, packedQuantity, actor);
            order = await GetPickingOrderHandler.FindPublished(orderId, store, false, ct);
        }
        var assignments = await store.ListPalletAssignmentsAsync(orderId, ct);
        return GetPickingOrderHandler.Map(order, actor, assignments);
    }

    private async Task<PickingOrderDetailsDto> LoadMapped(Guid orderId, CurrentActor actor, CancellationToken ct)
    {
        var order = await GetPickingOrderHandler.FindPublished(orderId, store, false, ct);
        var assignments = await store.ListPalletAssignmentsAsync(orderId, ct);
        return GetPickingOrderHandler.Map(order, actor, assignments);
    }

    private static void EnsureCanExecute(Order order, CurrentActor actor)
    {
        if (actor.Kind != ActorKind.Administrator && !GetPickingOrderHandler.IsAssigned(order, actor))
            throw new AccessDeniedException("This employee is not assigned to the order.");
    }

    private static void EnsureSameOperation(OrderPickingEvent entry, Guid orderId, Guid itemId,
        PickingAction action, decimal? packedQuantity, CurrentActor actor)
    {
        if (entry.OrderId != orderId || entry.OrderItemId != itemId || entry.Action != action ||
            entry.ActorKind != actor.Kind || entry.ActorId != actor.Id ||
            (action == PickingAction.Packed && entry.PackedQuantity != packedQuantity))
            throw new ResourceConflictException("operationId has already been used for another operation.");
    }
}

public sealed class ReservePickingItemHandler(IPickingStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : PickingMutationHandlerBase(store, authorization, audits, time),
    IRequestHandler<ReservePickingItemCommand, PickingOrderDetailsDto>
{
    public Task<PickingOrderDetailsDto> Handle(ReservePickingItemCommand request, CancellationToken ct) =>
        Execute(request.OrderId, request.ItemId, request.OperationId, request.ItemVersion,
            PickingAction.Reserved, null, ct);
}

public sealed class ReleasePickingItemHandler(IPickingStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : PickingMutationHandlerBase(store, authorization, audits, time),
    IRequestHandler<ReleasePickingItemCommand, PickingOrderDetailsDto>
{
    public Task<PickingOrderDetailsDto> Handle(ReleasePickingItemCommand request, CancellationToken ct) =>
        Execute(request.OrderId, request.ItemId, request.OperationId, request.ItemVersion,
            PickingAction.Released, null, ct);
}

public sealed class PackPickingItemHandler(IPickingStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : PickingMutationHandlerBase(store, authorization, audits, time),
    IRequestHandler<PackPickingItemCommand, PickingOrderDetailsDto>
{
    public Task<PickingOrderDetailsDto> Handle(PackPickingItemCommand request, CancellationToken ct) =>
        Execute(request.OrderId, request.ItemId, request.OperationId, request.ItemVersion,
            PickingAction.Packed, request.PackedQuantity, ct);
}

public sealed class UndoPackedPickingItemHandler(IPickingStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : PickingMutationHandlerBase(store, authorization, audits, time),
    IRequestHandler<UndoPackedPickingItemCommand, PickingOrderDetailsDto>
{
    public Task<PickingOrderDetailsDto> Handle(UndoPackedPickingItemCommand request, CancellationToken ct) =>
        Execute(request.OrderId, request.ItemId, request.OperationId, request.ItemVersion,
            PickingAction.PackingUndone, null, ct);
}
