using MediatR;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Application.WarehouseOrders;

public sealed record ListWarehouseOrdersQuery(int Page, int PageSize) : IRequest<PagedResult<WarehouseOrderListItemDto>>;
public sealed record GetWarehouseOrderQuery(Guid Id) : IRequest<WarehouseOrderDto>;
public sealed record ListAvailableWarehouseOrderAssigneesQuery : IRequest<IReadOnlyList<AvailableWarehouseOrderAssigneeDto>>;
public sealed record CreateWarehouseOrderItemInput(int ProductId, decimal Quantity);
public sealed record CreateWarehouseOrderCommand(string CustomerName, DateOnly DueDate, PickingMode PickingMode,
    IReadOnlyCollection<Guid> EmployeeIds, IReadOnlyCollection<CreateWarehouseOrderItemInput> Items) : IRequest<WarehouseOrderDto>;
public sealed record UpdateWarehouseOrderCommand(Guid Id, string CustomerName, DateOnly DueDate, long Version) : IRequest<WarehouseOrderDto>;
public sealed record AddWarehouseOrderItemCommand(Guid WarehouseOrderId, int ProductId, decimal Quantity, long Version) : IRequest<WarehouseOrderDto>;
public sealed record RemoveWarehouseOrderItemCommand(Guid WarehouseOrderId, Guid ItemId, long Version) : IRequest<WarehouseOrderDto>;
public sealed record PublishWarehouseOrderCommand(Guid WarehouseOrderId, long Version) : IRequest<WarehouseOrderDto>;
public sealed record DeleteWarehouseOrderCommand(Guid WarehouseOrderId, long Version) : IRequest;
public sealed record ConfigureWarehouseOrderPickingCommand(Guid WarehouseOrderId, PickingMode PickingMode,
    IReadOnlyCollection<Guid> EmployeeIds, long Version) : IRequest<WarehouseOrderDto>;

public sealed class ListWarehouseOrdersHandler(IWarehouseOrderStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<ListWarehouseOrdersQuery, PagedResult<WarehouseOrderListItemDto>>
{
    public Task<PagedResult<WarehouseOrderListItemDto>> Handle(ListWarehouseOrdersQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.WarehouseOrdersManage);
        ValidatePage(request.Page, request.PageSize);
        return store.ListAsync(request.Page, request.PageSize, ct);
    }

    internal static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new RequestValidationException("Page must be at least 1 and pageSize must be between 1 and 100.");
    }
}

public sealed class GetWarehouseOrderHandler(IWarehouseOrderStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<GetWarehouseOrderQuery, WarehouseOrderDto>
{
    public async Task<WarehouseOrderDto> Handle(GetWarehouseOrderQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.WarehouseOrdersManage);
        return Map(await Find(request.Id, store, false, ct));
    }

    internal static async Task<WarehouseOrder> Find(Guid id, IWarehouseOrderStore store, bool tracking, CancellationToken ct) =>
        await store.FindAsync(id, tracking, ct) ?? throw new ResourceNotFoundException("Warehouse order was not found.");

    internal static WarehouseOrderDto Map(WarehouseOrder warehouseOrder) => new(warehouseOrder.Id, warehouseOrder.Number, warehouseOrder.CustomerName, warehouseOrder.DueDate,
        warehouseOrder.Status, warehouseOrder.CreatedById, warehouseOrder.CreatedByName, warehouseOrder.CreatedAtUtc, warehouseOrder.UpdatedById,
        warehouseOrder.UpdatedByName, warehouseOrder.UpdatedAtUtc, warehouseOrder.PublishedAtUtc, warehouseOrder.Version, warehouseOrder.PickingMode,
        warehouseOrder.CustomerOrderId, warehouseOrder.CustomerDeliveryNoteNumber, warehouseOrder.SubiektSourceDocumentId,
        warehouseOrder.SubiektSourceDocumentNumber,
        warehouseOrder.Assignees.OrderBy(x => x.EmployeeDisplayName).Select(x => new WarehouseOrderAssigneeDto(
            x.EmployeeId, x.OrganizationId, x.EmployeeDisplayName, x.AssignedById,
            x.AssignedByName, x.AssignedAtUtc)).ToList(),
        warehouseOrder.Items.OrderBy(x => x.ProductName).Select(x => new WarehouseOrderItemDto(x.Id, x.ProductId,
            x.ProductName, x.ProductSymbol, x.Quantity, x.Unit, x.UnitWeightKg, x.Status, x.CustomerOrderItemId,
            x.CustomerPartNumber, x.EngineeringChange, x.DefaultPackagingTypeId, x.CustomerPackagingCode,
            x.SubiektSourceItemId)).ToList());
}

public sealed class ListAvailableWarehouseOrderAssigneesHandler(IWarehouseOrderWorkforceDirectory workforce,
    IApplicationAuthorizationService authorization)
    : IRequestHandler<ListAvailableWarehouseOrderAssigneesQuery, IReadOnlyList<AvailableWarehouseOrderAssigneeDto>>
{
    public Task<IReadOnlyList<AvailableWarehouseOrderAssigneeDto>> Handle(
        ListAvailableWarehouseOrderAssigneesQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.WarehouseOrdersManage);
        return workforce.ListAvailableAsync(ct);
    }
}

public sealed class CreateWarehouseOrderHandler(IWarehouseOrderStore store, IWarehouseOrderNumberGenerator numbers, IWarehouseOrderWorkforceDirectory workforce,
    IProductReadRepository products, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<CreateWarehouseOrderCommand, WarehouseOrderDto>
{
    public async Task<WarehouseOrderDto> Handle(CreateWarehouseOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersManage);
        var now = time.GetUtcNow();
        var id = Guid.NewGuid();
        var assignees = await workforce.ResolveActiveAsync(request.EmployeeIds, ct);
        if (assignees.Count != request.EmployeeIds.Distinct().Count())
            throw new RequestValidationException("Every assigned employee must be active and belong to an active organization.");
        if (request.Items.Select(x => x.ProductId).Distinct().Count() != request.Items.Count)
            throw new RequestValidationException("A product can be added to an order only once.");
        try
        {
            var warehouseOrder = WarehouseOrder.Create(id, numbers.Generate(id, now), request.CustomerName, request.DueDate,
                actor.Id, actor.DisplayName, now, request.PickingMode, assignees);
            foreach (var input in request.Items)
            {
                var product = await products.GetProductWarehouseOrderSnapshotAsync(input.ProductId, ct)
                    ?? throw new ResourceNotFoundException("Product was not found.");
                warehouseOrder.AddItem(product.Id, product.Name, product.Symbol, input.Quantity, product.Unit,
                    product.UnitWeightKg, actor.Id, actor.DisplayName, now);
            }
            var result = await store.AddAsync(warehouseOrder, audits.Create(actor, "WarehouseOrderCreated", "WarehouseOrder", id, now), ct);
            if (result != WarehouseOrderStoreResult.Success) throw new ResourceConflictException("Warehouse order could not be created.");
            return GetWarehouseOrderHandler.Map(warehouseOrder);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { throw new RequestValidationException(ex.Message); }
    }
}

public sealed class ConfigureWarehouseOrderPickingHandler(IWarehouseOrderStore store, IWarehouseOrderWorkforceDirectory workforce,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<ConfigureWarehouseOrderPickingCommand, WarehouseOrderDto>
{
    public async Task<WarehouseOrderDto> Handle(ConfigureWarehouseOrderPickingCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersManage);
        var warehouseOrder = await GetWarehouseOrderHandler.Find(request.WarehouseOrderId, store, true, ct);
        UpdateWarehouseOrderHandler.EnsureVersion(warehouseOrder, request.Version);
        var assignees = await workforce.ResolveActiveAsync(request.EmployeeIds, ct);
        if (assignees.Count != request.EmployeeIds.Distinct().Count())
            throw new RequestValidationException("Every assigned employee must be active and belong to an active organization.");
        var now = time.GetUtcNow();
        try { warehouseOrder.ConfigurePicking(request.PickingMode, assignees, actor.Id, actor.DisplayName, now); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { throw new RequestValidationException(ex.Message); }
        await UpdateWarehouseOrderHandler.Save(store, warehouseOrder, request.Version,
            audits.Create(actor, "WarehouseOrderPickingConfigured", "WarehouseOrder", warehouseOrder.Id, now), ct);
        return GetWarehouseOrderHandler.Map(warehouseOrder);
    }
}

public sealed class UpdateWarehouseOrderHandler(IWarehouseOrderStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<UpdateWarehouseOrderCommand, WarehouseOrderDto>
{
    public async Task<WarehouseOrderDto> Handle(UpdateWarehouseOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersManage);
        var warehouseOrder = await GetWarehouseOrderHandler.Find(request.Id, store, true, ct);
        EnsureVersion(warehouseOrder, request.Version);
        var now = time.GetUtcNow();
        try { warehouseOrder.UpdateHeader(request.CustomerName, request.DueDate, actor.Id, actor.DisplayName, now); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { throw new RequestValidationException(ex.Message); }
        await Save(store, warehouseOrder, request.Version, audits.Create(actor, "WarehouseOrderUpdated", "WarehouseOrder", warehouseOrder.Id, now), ct);
        return GetWarehouseOrderHandler.Map(warehouseOrder);
    }

    internal static void EnsureVersion(WarehouseOrder warehouseOrder, long version)
    {
        if (version <= 0 || warehouseOrder.Version != version) throw new ResourceConflictException("Warehouse order was modified by another request.");
    }

    internal static async Task Save(IWarehouseOrderStore store, WarehouseOrder warehouseOrder, long expectedVersion,
        Domain.Identity.AuditEntry audit, CancellationToken ct)
    {
        var result = await store.SaveAsync(warehouseOrder, expectedVersion, audit, ct);
        if (result == WarehouseOrderStoreResult.Conflict) throw new ResourceConflictException("Warehouse order was modified by another request.");
        if (result == WarehouseOrderStoreResult.NotFound) throw new ResourceNotFoundException("Warehouse order was not found.");
    }
}

public sealed class AddWarehouseOrderItemHandler(IWarehouseOrderStore store, IProductReadRepository products,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<AddWarehouseOrderItemCommand, WarehouseOrderDto>
{
    public async Task<WarehouseOrderDto> Handle(AddWarehouseOrderItemCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersManage);
        var warehouseOrder = await GetWarehouseOrderHandler.Find(request.WarehouseOrderId, store, true, ct);
        UpdateWarehouseOrderHandler.EnsureVersion(warehouseOrder, request.Version);
        var product = await products.GetProductWarehouseOrderSnapshotAsync(request.ProductId, ct)
            ?? throw new ResourceNotFoundException("Product was not found.");
        var now = time.GetUtcNow();
        try { warehouseOrder.AddItem(product.Id, product.Name, product.Symbol, request.Quantity, product.Unit,
            product.UnitWeightKg, actor.Id, actor.DisplayName, now); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { throw new RequestValidationException(ex.Message); }
        await UpdateWarehouseOrderHandler.Save(store, warehouseOrder, request.Version,
            audits.Create(actor, "WarehouseOrderItemAdded", "WarehouseOrder", warehouseOrder.Id, now), ct);
        return GetWarehouseOrderHandler.Map(warehouseOrder);
    }
}

public sealed class RemoveWarehouseOrderItemHandler(IWarehouseOrderStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<RemoveWarehouseOrderItemCommand, WarehouseOrderDto>
{
    public async Task<WarehouseOrderDto> Handle(RemoveWarehouseOrderItemCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersManage);
        var warehouseOrder = await GetWarehouseOrderHandler.Find(request.WarehouseOrderId, store, true, ct);
        UpdateWarehouseOrderHandler.EnsureVersion(warehouseOrder, request.Version);
        var now = time.GetUtcNow();
        try { warehouseOrder.RemoveItem(request.ItemId, actor.Id, actor.DisplayName, now); }
        catch (KeyNotFoundException) { throw new ResourceNotFoundException("Warehouse order item was not found."); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }
        await UpdateWarehouseOrderHandler.Save(store, warehouseOrder, request.Version,
            audits.Create(actor, "WarehouseOrderItemRemoved", "WarehouseOrder", warehouseOrder.Id, now), ct);
        return GetWarehouseOrderHandler.Map(warehouseOrder);
    }
}

public sealed class PublishWarehouseOrderHandler(IWarehouseOrderStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<PublishWarehouseOrderCommand, WarehouseOrderDto>
{
    public async Task<WarehouseOrderDto> Handle(PublishWarehouseOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersManage);
        var warehouseOrder = await GetWarehouseOrderHandler.Find(request.WarehouseOrderId, store, true, ct);
        UpdateWarehouseOrderHandler.EnsureVersion(warehouseOrder, request.Version);
        var now = time.GetUtcNow();
        try { warehouseOrder.Publish(DateOnly.FromDateTime(now.UtcDateTime), actor.Id, actor.DisplayName, now); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }
        await UpdateWarehouseOrderHandler.Save(store, warehouseOrder, request.Version,
            audits.Create(actor, "WarehouseOrderPublished", "WarehouseOrder", warehouseOrder.Id, now), ct);
        return GetWarehouseOrderHandler.Map(warehouseOrder);
    }
}

public sealed class DeleteWarehouseOrderHandler(IWarehouseOrderStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<DeleteWarehouseOrderCommand>
{
    public async Task Handle(DeleteWarehouseOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.WarehouseOrdersManage);
        var warehouseOrder = await GetWarehouseOrderHandler.Find(request.WarehouseOrderId, store, true, ct);
        UpdateWarehouseOrderHandler.EnsureVersion(warehouseOrder, request.Version);
        try { warehouseOrder.EnsureCanDelete(); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }

        var result = await store.DeleteAsync(warehouseOrder, request.Version,
            audits.Create(actor, "WarehouseOrderDeleted", "WarehouseOrder", warehouseOrder.Id, time.GetUtcNow()), ct);
        if (result == WarehouseOrderStoreResult.Conflict)
            throw new ResourceConflictException("Warehouse order was modified by another request.");
        if (result == WarehouseOrderStoreResult.NotFound)
            throw new ResourceNotFoundException("Warehouse order was not found.");
    }
}

public sealed class WarehouseOrderNumberGenerator : IWarehouseOrderNumberGenerator
{
    public string Generate(Guid warehouseOrderId, DateTimeOffset now) => $"ZAM-{now:yyyyMMdd}-{warehouseOrderId.ToString("N")[..8].ToUpperInvariant()}";
}
