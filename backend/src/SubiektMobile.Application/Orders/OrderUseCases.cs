using MediatR;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Orders;

namespace SubiektMobile.Application.Orders;

public sealed record ListOrdersQuery(int Page, int PageSize) : IRequest<PagedResult<OrderListItemDto>>;
public sealed record GetOrderQuery(Guid Id) : IRequest<OrderDto>;
public sealed record CreateOrderCommand(string CustomerName, DateOnly DueDate) : IRequest<OrderDto>;
public sealed record UpdateOrderCommand(Guid Id, string CustomerName, DateOnly DueDate, long Version) : IRequest<OrderDto>;
public sealed record AddOrderItemCommand(Guid OrderId, int ProductId, decimal Quantity, long Version) : IRequest<OrderDto>;
public sealed record RemoveOrderItemCommand(Guid OrderId, Guid ItemId, long Version) : IRequest<OrderDto>;
public sealed record PublishOrderCommand(Guid OrderId, long Version) : IRequest<OrderDto>;

public sealed class ListOrdersHandler(IOrderStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<ListOrdersQuery, PagedResult<OrderListItemDto>>
{
    public Task<PagedResult<OrderListItemDto>> Handle(ListOrdersQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.OrdersManage);
        ValidatePage(request.Page, request.PageSize);
        return store.ListAsync(request.Page, request.PageSize, ct);
    }

    internal static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new RequestValidationException("Page must be at least 1 and pageSize must be between 1 and 100.");
    }
}

public sealed class GetOrderHandler(IOrderStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.OrdersManage);
        return Map(await Find(request.Id, store, false, ct));
    }

    internal static async Task<Order> Find(Guid id, IOrderStore store, bool tracking, CancellationToken ct) =>
        await store.FindAsync(id, tracking, ct) ?? throw new ResourceNotFoundException("Order was not found.");

    internal static OrderDto Map(Order order) => new(order.Id, order.Number, order.CustomerName, order.DueDate,
        order.Status, order.CreatedById, order.CreatedByName, order.CreatedAtUtc, order.UpdatedById,
        order.UpdatedByName, order.UpdatedAtUtc, order.PublishedAtUtc, order.Version,
        order.Items.OrderBy(x => x.ProductName).Select(x => new OrderItemDto(x.Id, x.ProductId,
            x.ProductName, x.ProductSymbol, x.Quantity, x.Unit, x.UnitWeightKg, x.Status)).ToList());
}

public sealed class CreateOrderHandler(IOrderStore store, IOrderNumberGenerator numbers,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.OrdersManage);
        var now = time.GetUtcNow();
        var id = Guid.NewGuid();
        try
        {
            var order = Order.Create(id, numbers.Generate(id, now), request.CustomerName, request.DueDate,
                actor.Id, actor.DisplayName, now);
            var result = await store.AddAsync(order, audits.Create(actor, "OrderCreated", "Order", id, now), ct);
            if (result != OrderStoreResult.Success) throw new ResourceConflictException("Order could not be created.");
            return GetOrderHandler.Map(order);
        }
        catch (ArgumentException ex) { throw new RequestValidationException(ex.Message); }
    }
}

public sealed class UpdateOrderHandler(IOrderStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<UpdateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.OrdersManage);
        var order = await GetOrderHandler.Find(request.Id, store, true, ct);
        EnsureVersion(order, request.Version);
        var now = time.GetUtcNow();
        try { order.UpdateHeader(request.CustomerName, request.DueDate, actor.Id, actor.DisplayName, now); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { throw new RequestValidationException(ex.Message); }
        await Save(store, order, request.Version, audits.Create(actor, "OrderUpdated", "Order", order.Id, now), ct);
        return GetOrderHandler.Map(order);
    }

    internal static void EnsureVersion(Order order, long version)
    {
        if (version <= 0 || order.Version != version) throw new ResourceConflictException("Order was modified by another request.");
    }

    internal static async Task Save(IOrderStore store, Order order, long expectedVersion,
        Domain.Identity.AuditEntry audit, CancellationToken ct)
    {
        var result = await store.SaveAsync(order, expectedVersion, audit, ct);
        if (result == OrderStoreResult.Conflict) throw new ResourceConflictException("Order was modified by another request.");
        if (result == OrderStoreResult.NotFound) throw new ResourceNotFoundException("Order was not found.");
    }
}

public sealed class AddOrderItemHandler(IOrderStore store, IProductReadRepository products,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<AddOrderItemCommand, OrderDto>
{
    public async Task<OrderDto> Handle(AddOrderItemCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.OrdersManage);
        var order = await GetOrderHandler.Find(request.OrderId, store, true, ct);
        UpdateOrderHandler.EnsureVersion(order, request.Version);
        var product = await products.GetProductOrderSnapshotAsync(request.ProductId, ct)
            ?? throw new ResourceNotFoundException("Product was not found.");
        var now = time.GetUtcNow();
        try { order.AddItem(product.Id, product.Name, product.Symbol, request.Quantity, product.Unit,
            product.UnitWeightKg, actor.Id, actor.DisplayName, now); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { throw new RequestValidationException(ex.Message); }
        await UpdateOrderHandler.Save(store, order, request.Version,
            audits.Create(actor, "OrderItemAdded", "Order", order.Id, now), ct);
        return GetOrderHandler.Map(order);
    }
}

public sealed class RemoveOrderItemHandler(IOrderStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<RemoveOrderItemCommand, OrderDto>
{
    public async Task<OrderDto> Handle(RemoveOrderItemCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.OrdersManage);
        var order = await GetOrderHandler.Find(request.OrderId, store, true, ct);
        UpdateOrderHandler.EnsureVersion(order, request.Version);
        var now = time.GetUtcNow();
        try { order.RemoveItem(request.ItemId, actor.Id, actor.DisplayName, now); }
        catch (KeyNotFoundException) { throw new ResourceNotFoundException("Order item was not found."); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }
        await UpdateOrderHandler.Save(store, order, request.Version,
            audits.Create(actor, "OrderItemRemoved", "Order", order.Id, now), ct);
        return GetOrderHandler.Map(order);
    }
}

public sealed class PublishOrderHandler(IOrderStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<PublishOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(PublishOrderCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.OrdersManage);
        var order = await GetOrderHandler.Find(request.OrderId, store, true, ct);
        UpdateOrderHandler.EnsureVersion(order, request.Version);
        var now = time.GetUtcNow();
        try { order.Publish(DateOnly.FromDateTime(now.UtcDateTime), actor.Id, actor.DisplayName, now); }
        catch (InvalidOperationException ex) { throw new RequestValidationException(ex.Message); }
        await UpdateOrderHandler.Save(store, order, request.Version,
            audits.Create(actor, "OrderPublished", "Order", order.Id, now), ct);
        return GetOrderHandler.Map(order);
    }
}

public sealed class OrderNumberGenerator : IOrderNumberGenerator
{
    public string Generate(Guid orderId, DateTimeOffset now) => $"ZAM-{now:yyyyMMdd}-{orderId.ToString("N")[..8].ToUpperInvariant()}";
}
