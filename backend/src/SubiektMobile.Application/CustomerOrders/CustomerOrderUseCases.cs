using MediatR;
using SubiektMobile.Application.Customers;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Application.WarehouseOrders;
using SubiektMobile.Domain.CustomerOrders;
using SubiektMobile.Domain.Customers;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Application.CustomerOrders;

public sealed record CreateCustomerOrderItemInput(string CustomerPartNumber, decimal Quantity);
public sealed record ListCustomerOrdersQuery(string? Search, CustomerOrderStatus? Status, Guid? CustomerId,
    Guid? CustomerSiteId, DateOnly? DueDateFrom, DateOnly? DueDateTo, int Page, int PageSize)
    : IRequest<PagedResult<CustomerOrderListItemDto>>;
public sealed record GetCustomerOrderQuery(Guid CustomerOrderId) : IRequest<CustomerOrderDto>;
public sealed record CreateCustomerOrderCommand(Guid CustomerId, Guid CustomerSiteId, string? CustomerOrderNumber,
    string? DeliveryNoteNumber, DateOnly RequestedDeliveryDate, string? CustomerNotes,
    IReadOnlyCollection<CreateCustomerOrderItemInput> Items) : IRequest<CustomerOrderDto>;
public sealed record UpdateCustomerOrderCommand(Guid CustomerOrderId, string? CustomerOrderNumber,
    string? DeliveryNoteNumber, DateOnly RequestedDeliveryDate, string? CustomerNotes, long Version)
    : IRequest<CustomerOrderDto>;
public sealed record AddCustomerOrderItemCommand(Guid CustomerOrderId, string CustomerPartNumber, decimal Quantity,
    long Version) : IRequest<CustomerOrderDto>;
public sealed record UpdateCustomerOrderItemCommand(Guid CustomerOrderId, Guid ItemId, string CustomerPartNumber,
    decimal Quantity, long Version) : IRequest<CustomerOrderDto>;
public sealed record RemoveCustomerOrderItemCommand(Guid CustomerOrderId, Guid ItemId, long Version)
    : IRequest<CustomerOrderDto>;
public sealed record GetCustomerOrderReadinessQuery(Guid CustomerOrderId) : IRequest<CustomerOrderReadinessDto>;
public sealed record ListCustomerOrderActivityQuery(Guid CustomerOrderId, int Page, int PageSize)
    : IRequest<PagedResult<CustomerOrderActivityDto>>;
public sealed record MarkCustomerOrderReadyCommand(Guid CustomerOrderId, long Version) : IRequest<CustomerOrderDto>;
public sealed record CancelCustomerOrderCommand(Guid CustomerOrderId, long Version) : IRequest<CustomerOrderDto>;
public sealed record ConvertCustomerOrderCommand(Guid CustomerOrderId, long Version) : IRequest<CustomerOrderConversionDto>;

public sealed class CustomerOrderHandlers(ICustomerOrderStore store, ICustomerStore customers, IPackagingStore packaging,
    IProductReadRepository products, IWarehouseOrderNumberGenerator numbers,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time) :
    IRequestHandler<ListCustomerOrdersQuery, PagedResult<CustomerOrderListItemDto>>,
    IRequestHandler<GetCustomerOrderQuery, CustomerOrderDto>,
    IRequestHandler<CreateCustomerOrderCommand, CustomerOrderDto>,
    IRequestHandler<UpdateCustomerOrderCommand, CustomerOrderDto>,
    IRequestHandler<AddCustomerOrderItemCommand, CustomerOrderDto>,
    IRequestHandler<UpdateCustomerOrderItemCommand, CustomerOrderDto>,
    IRequestHandler<RemoveCustomerOrderItemCommand, CustomerOrderDto>,
    IRequestHandler<GetCustomerOrderReadinessQuery, CustomerOrderReadinessDto>,
    IRequestHandler<ListCustomerOrderActivityQuery, PagedResult<CustomerOrderActivityDto>>,
    IRequestHandler<MarkCustomerOrderReadyCommand, CustomerOrderDto>,
    IRequestHandler<CancelCustomerOrderCommand, CustomerOrderDto>,
    IRequestHandler<ConvertCustomerOrderCommand, CustomerOrderConversionDto>
{
    public Task<PagedResult<CustomerOrderListItemDto>> Handle(ListCustomerOrdersQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomerOrdersManage);
        CustomerOrderUseCases.ValidatePage(request.Page, request.PageSize);
        if (request.DueDateFrom is not null && request.DueDateTo is not null && request.DueDateFrom > request.DueDateTo)
            throw new RequestValidationException("Due date range is invalid.");
        return store.ListAsync(request.Search, request.Status, request.CustomerId, request.CustomerSiteId,
            request.DueDateFrom, request.DueDateTo, request.Page, request.PageSize, ct);
    }

    public async Task<CustomerOrderDto> Handle(GetCustomerOrderQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomerOrdersManage);
        var order = await Find(request.CustomerOrderId, false, ct);
        return await Map(order, ct);
    }

    public async Task<CustomerOrderDto> Handle(CreateCustomerOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        var site = await EnsureActiveContext(request.CustomerId, request.CustomerSiteId, ct);
        var now = time.GetUtcNow();
        try
        {
            var order = CustomerOrder.Create(Guid.NewGuid(), request.CustomerId, site.Id, request.CustomerOrderNumber,
                request.DeliveryNoteNumber, request.RequestedDeliveryDate, request.CustomerNotes,
                actor.Id, actor.DisplayName, now);
            foreach (var item in request.Items)
                order.AddItem(Guid.NewGuid(), item.CustomerPartNumber, item.Quantity, actor.Id, actor.DisplayName, now);
            EnsureSaved(await store.AddAsync(order,
                audits.Create(actor, "CustomerOrderCreated", "CustomerOrder", order.Id, now), ct));
            return await Map(order, ct);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            throw new RequestValidationException(ex.Message);
        }
    }

    public async Task<CustomerOrderDto> Handle(UpdateCustomerOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        var order = await Find(request.CustomerOrderId, true, ct);
        EnsureVersion(order, request.Version);
        var now = time.GetUtcNow();
        try { order.Update(request.CustomerOrderNumber, request.DeliveryNoteNumber, request.RequestedDeliveryDate, request.CustomerNotes, actor.Id, actor.DisplayName, now); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { throw new RequestValidationException(ex.Message); }
        await Save(order, request.Version, audits.Create(actor, "CustomerOrderUpdated", "CustomerOrder", order.Id, now), ct);
        return await Map(order, ct);
    }

    public async Task<CustomerOrderDto> Handle(AddCustomerOrderItemCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        var order = await Find(request.CustomerOrderId, true, ct);
        EnsureVersion(order, request.Version);
        var now = time.GetUtcNow();
        CustomerOrderItem item;
        try { item = order.AddItem(Guid.NewGuid(), request.CustomerPartNumber, request.Quantity, actor.Id, actor.DisplayName, now); }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException) { throw new RequestValidationException(ex.Message); }
        await Save(order, request.Version, audits.Create(actor, "CustomerOrderItemAdded", "CustomerOrderItem", item.Id, now), ct);
        return await Map(order, ct);
    }

    public async Task<CustomerOrderDto> Handle(UpdateCustomerOrderItemCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        var order = await Find(request.CustomerOrderId, true, ct);
        EnsureVersion(order, request.Version);
        var now = time.GetUtcNow();
        try { order.UpdateItem(request.ItemId, request.CustomerPartNumber, request.Quantity, actor.Id, actor.DisplayName, now); }
        catch (KeyNotFoundException) { throw new ResourceNotFoundException("Customer order item was not found."); }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException) { throw new RequestValidationException(ex.Message); }
        await Save(order, request.Version, audits.Create(actor, "CustomerOrderItemUpdated", "CustomerOrderItem", request.ItemId, now), ct);
        return await Map(order, ct);
    }

    public async Task<CustomerOrderDto> Handle(RemoveCustomerOrderItemCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        var order = await Find(request.CustomerOrderId, true, ct);
        EnsureVersion(order, request.Version);
        var now = time.GetUtcNow();
        try { order.RemoveItem(request.ItemId, actor.Id, actor.DisplayName, now); }
        catch (KeyNotFoundException) { throw new ResourceNotFoundException("Customer order item was not found."); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }
        await Save(order, request.Version, audits.Create(actor, "CustomerOrderItemRemoved", "CustomerOrderItem", request.ItemId, now), ct);
        return await Map(order, ct);
    }

    public async Task<CustomerOrderReadinessDto> Handle(GetCustomerOrderReadinessQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomerOrdersManage);
        return await Assess(await Find(request.CustomerOrderId, false, ct), ct);
    }

    public async Task<PagedResult<CustomerOrderActivityDto>> Handle(ListCustomerOrderActivityQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomerOrdersManage);
        CustomerOrderUseCases.ValidatePage(request.Page, request.PageSize);
        _ = await Find(request.CustomerOrderId, false, ct);
        return await store.ListActivityAsync(request.CustomerOrderId, request.Page, request.PageSize, ct);
    }

    public async Task<CustomerOrderDto> Handle(MarkCustomerOrderReadyCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        var order = await Find(request.CustomerOrderId, true, ct);
        EnsureVersion(order, request.Version);
        var readiness = await Assess(order, ct);
        if (!readiness.CanConvert) throw new RequestValidationException("Customer order is not ready for conversion.");
        var now = time.GetUtcNow();
        try { order.MarkReady(actor.Id, actor.DisplayName, now); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }
        await Save(order, request.Version, audits.Create(actor, "CustomerOrderReadyForConversion", "CustomerOrder", order.Id, now), ct);
        return await Map(order, ct);
    }

    public async Task<CustomerOrderDto> Handle(CancelCustomerOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        var order = await Find(request.CustomerOrderId, true, ct);
        EnsureVersion(order, request.Version);
        var now = time.GetUtcNow();
        try { order.Cancel(actor.Id, actor.DisplayName, now); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }
        await Save(order, request.Version, audits.Create(actor, "CustomerOrderCancelled", "CustomerOrder", order.Id, now), ct);
        return await Map(order, ct);
    }

    public async Task<CustomerOrderConversionDto> Handle(ConvertCustomerOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        authorization.Require(Permissions.WarehouseOrdersManage);
        var order = await Find(request.CustomerOrderId, true, ct);
        EnsureVersion(order, request.Version);
        var readiness = await Assess(order, ct);
        if (!readiness.CanConvert) throw new RequestValidationException("Customer order is not ready for conversion.");
        var customer = await CustomerUseCases.FindCustomer(order.CustomerId, customers, false, ct);
        var now = time.GetUtcNow();
        var warehouseOrderId = Guid.NewGuid();
        var warehouseOrder = WarehouseOrder.Create(warehouseOrderId, numbers.Generate(warehouseOrderId, now), customer.Name,
            order.RequestedDeliveryDate, actor.Id, actor.DisplayName, now, customerOrderId: order.Id,
            customerDeliveryNoteNumber: order.DeliveryNoteNumber);
        foreach (var item in order.Items)
        {
            var resolved = readiness.Items.Single(x => x.CustomerOrderItemId == item.Id);
            var product = await products.GetProductWarehouseOrderSnapshotAsync(resolved.ProductId!.Value, ct)
                ?? throw new RequestValidationException("A mapped product is no longer available.");
            warehouseOrder.AddCustomerOrderItem(new WarehouseOrderCustomerItemSource(item.Id, item.CustomerPartNumber,
                resolved.EngineeringChange, resolved.DefaultPackagingTypeId, resolved.CustomerPackagingCode), product.Id,
                product.Name, product.Symbol, item.Quantity, product.Unit, product.UnitWeightKg,
                actor.Id, actor.DisplayName, now);
        }
        try { order.MarkConverted(actor.Id, actor.DisplayName, now); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }
        EnsureSaved(await store.ConvertAsync(order, warehouseOrder, request.Version,
            audits.Create(actor, "CustomerOrderConverted", "CustomerOrder", order.Id, now),
            audits.Create(actor, "WarehouseOrderCreatedFromCustomerOrder", "WarehouseOrder", warehouseOrder.Id, now), ct));
        return new CustomerOrderConversionDto(await Map(order, ct), warehouseOrder.Id, warehouseOrder.Number);
    }

    private async Task<CustomerOrderReadinessDto> Assess(CustomerOrder order, CancellationToken ct)
    {
        var issues = new List<CustomerOrderReadinessIssueDto>();
        var customer = await CustomerUseCases.FindCustomer(order.CustomerId, customers, false, ct);
        var site = await CustomerUseCases.FindSite(order.CustomerId, order.CustomerSiteId, customers, false, ct);
        if (!customer.IsActive) issues.Add(Block("CustomerInactive", null, "Customer is inactive."));
        if (!site.IsActive) issues.Add(Block("CustomerSiteInactive", null, "Customer site is inactive."));
        if (site.LogisticsProfile?.IsComplete != true)
            issues.Add(Block("LogisticsProfileIncomplete", null, "Customer site logistics profile is incomplete."));
        if (order.Items.Count == 0) issues.Add(Block("NoItems", null, "Customer order has no items."));
        var items = new List<CustomerOrderItemResolutionDto>();
        foreach (var item in order.Items)
        {
            var resolution = await packaging.ResolveAsync(order.CustomerId, order.CustomerSiteId, item.CustomerPartNumber, ct);
            var mapping = resolution.Mapping;
            ProductWarehouseOrderSnapshot? product = null;
            if (mapping is null)
                issues.Add(Block("MappingMissing", item.Id, "Customer part mapping is missing."));
            else
            {
                product = await products.GetProductWarehouseOrderSnapshotAsync(mapping.ProductId, ct);
                if (product is null)
                    issues.Add(Block("ProductUnavailable", item.Id, "Mapped product is unavailable."));
                if (mapping.DefaultPackagingTypeId is null)
                    issues.Add(Warn("PackagingMissing", item.Id, "Default packaging is not configured."));
                else if (resolution.PackagingCode is null)
                    issues.Add(Warn("PackagingCodeMissing", item.Id, "Customer packaging code B is not configured."));
            }
            items.Add(new CustomerOrderItemResolutionDto(item.Id, item.CustomerPartNumber, resolution.Readiness,
                product?.Id, product?.Name, product?.Symbol, mapping?.EngineeringChange,
                mapping?.DefaultPackagingTypeId, resolution.PackagingCode?.Code));
        }
        return new CustomerOrderReadinessDto(!issues.Any(x => x.Severity == CustomerOrderReadinessSeverity.Blocking), issues, items);
    }

    private async Task<CustomerSite> EnsureActiveContext(Guid customerId, Guid siteId, CancellationToken ct)
    {
        var customer = await CustomerUseCases.FindCustomer(customerId, customers, false, ct);
        var site = await CustomerUseCases.FindSite(customerId, siteId, customers, false, ct);
        if (!customer.IsActive || !site.IsActive) throw new RequestValidationException("Customer and customer site must be active.");
        return site;
    }

    private async Task<CustomerOrderDto> Map(CustomerOrder order, CancellationToken ct)
    {
        var customer = await CustomerUseCases.FindCustomer(order.CustomerId, customers, false, ct);
        var site = await CustomerUseCases.FindSite(order.CustomerId, order.CustomerSiteId, customers, false, ct);
        return new CustomerOrderDto(order.Id, order.CustomerId, customer.Name, order.CustomerSiteId, site.Name,
            order.CustomerOrderNumber, order.DeliveryNoteNumber, order.RequestedDeliveryDate, order.CustomerNotes,
            order.Status, order.CreatedById, order.CreatedByName, order.CreatedAtUtc, order.UpdatedById,
            order.UpdatedByName, order.UpdatedAtUtc, order.Version,
            await store.FindWarehouseOrderIdAsync(order.Id, ct), order.Items.OrderBy(x => x.Id)
                .Select(x => new CustomerOrderItemDto(x.Id, x.CustomerPartNumber, x.Quantity)).ToList());
    }

    private async Task<CustomerOrder> Find(Guid id, bool tracking, CancellationToken ct) =>
        await store.FindAsync(id, tracking, ct) ?? throw new ResourceNotFoundException("Customer order was not found.");

    private async Task Save(CustomerOrder order, long version, AuditEntry audit, CancellationToken ct) =>
        EnsureSaved(await store.SaveAsync(order, version, audit, ct));

    private static void EnsureVersion(CustomerOrder order, long version)
    {
        if (version <= 0 || order.Version != version)
            throw new ResourceConflictException("Customer order was modified by another request.");
    }

    private static void EnsureSaved(CustomerOrderStoreResult result)
    {
        if (result == CustomerOrderStoreResult.Conflict)
            throw new ResourceConflictException("Customer order was modified by another request.");
        if (result == CustomerOrderStoreResult.NotFound)
            throw new ResourceNotFoundException("Customer order was not found.");
    }

    private static CustomerOrderReadinessIssueDto Block(string code, Guid? itemId, string message) =>
        new(CustomerOrderReadinessSeverity.Blocking, code, itemId, message);
    private static CustomerOrderReadinessIssueDto Warn(string code, Guid? itemId, string message) =>
        new(CustomerOrderReadinessSeverity.Warning, code, itemId, message);
}

internal static class CustomerOrderUseCases
{
    public static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new RequestValidationException("Page must be at least 1 and pageSize must be between 1 and 100.");
    }
}
