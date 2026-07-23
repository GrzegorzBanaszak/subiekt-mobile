using MediatR;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Application.WarehouseOrders;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Application.CustomerOrders;

public sealed record SubiektCustomerOrderListItemDto(int SourceDocumentId, string Number, string CustomerName,
    DateOnly RequestedDeliveryDate, int SourceStatus, int ItemCount, Guid? WarehouseOrderId);

public sealed record SubiektCustomerOrderItemDto(int SourceItemId, int ProductId, string ProductName,
    string? ProductSymbol, decimal Quantity, string Unit);

public sealed record SubiektCustomerOrderDto(int SourceDocumentId, string Number, string CustomerName,
    DateOnly RequestedDeliveryDate, DateOnly IssuedDate, int SourceStatus, string? Notes,
    Guid? WarehouseOrderId, IReadOnlyList<SubiektCustomerOrderItemDto> Items);

public interface ISubiektCustomerOrderReadRepository
{
    Task<PagedResult<SubiektCustomerOrderListItemDto>> ListAsync(string? search, bool includeCompleted,
        int page, int pageSize, CancellationToken cancellationToken);
    Task<SubiektCustomerOrderDto?> FindAsync(int sourceDocumentId, CancellationToken cancellationToken);
}

public sealed record ListSubiektCustomerOrdersQuery(string? Search, bool IncludeCompleted, int Page, int PageSize)
    : IRequest<PagedResult<SubiektCustomerOrderListItemDto>>;
public sealed record GetSubiektCustomerOrderQuery(int SourceDocumentId) : IRequest<SubiektCustomerOrderDto>;
public sealed record ConvertSubiektCustomerOrderCommand(int SourceDocumentId) : IRequest<SubiektCustomerOrderConversionDto>;
public sealed record SubiektCustomerOrderConversionDto(Guid WarehouseOrderId, string WarehouseOrderNumber,
    bool WasAlreadyConverted);

public sealed class SubiektCustomerOrderHandlers(ISubiektCustomerOrderReadRepository source,
    IWarehouseOrderStore warehouseOrders, IWarehouseOrderNumberGenerator numbers, IProductReadRepository products,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time) :
    IRequestHandler<ListSubiektCustomerOrdersQuery, PagedResult<SubiektCustomerOrderListItemDto>>,
    IRequestHandler<GetSubiektCustomerOrderQuery, SubiektCustomerOrderDto>,
    IRequestHandler<ConvertSubiektCustomerOrderCommand, SubiektCustomerOrderConversionDto>
{
    public async Task<PagedResult<SubiektCustomerOrderListItemDto>> Handle(ListSubiektCustomerOrdersQuery request,
        CancellationToken ct)
    {
        authorization.Require(Permissions.CustomerOrdersManage);
        CustomerOrderUseCases.ValidatePage(request.Page, request.PageSize);
        var page = await source.ListAsync(request.Search, request.IncludeCompleted, request.Page, request.PageSize, ct);
        var converted = await warehouseOrders.FindBySubiektSourceDocumentIdsAsync(
            page.Items.Select(x => x.SourceDocumentId).ToArray(), ct);
        return new PagedResult<SubiektCustomerOrderListItemDto>(page.Items.Select(x => x with
        {
            WarehouseOrderId = converted.TryGetValue(x.SourceDocumentId, out var warehouseOrderId)
                ? warehouseOrderId : null
        }).ToList(), page.Page, page.PageSize, page.TotalCount, page.TotalPages);
    }

    public async Task<SubiektCustomerOrderDto> Handle(GetSubiektCustomerOrderQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomerOrdersManage);
        var order = await FindSource(request.SourceDocumentId, ct);
        var converted = await warehouseOrders.FindBySubiektSourceDocumentIdAsync(order.SourceDocumentId, ct);
        return order with { WarehouseOrderId = converted?.Id };
    }

    public async Task<SubiektCustomerOrderConversionDto> Handle(ConvertSubiektCustomerOrderCommand request,
        CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomerOrdersManage);
        authorization.Require(Permissions.WarehouseOrdersManage);
        var sourceOrder = await FindSource(request.SourceDocumentId, ct);
        if (sourceOrder.SourceStatus is 0 or 2)
            throw new RequestValidationException("Cancelled source orders cannot be converted.");
        if (sourceOrder.Items.Count == 0)
            throw new RequestValidationException("The source order has no product items.");

        var existing = await warehouseOrders.FindBySubiektSourceDocumentIdAsync(sourceOrder.SourceDocumentId, ct);
        if (existing is not null)
            return new SubiektCustomerOrderConversionDto(existing.Value.Id, existing.Value.Number, true);

        var now = time.GetUtcNow();
        var warehouseOrderId = Guid.NewGuid();
        var warehouseOrder = WarehouseOrder.Create(warehouseOrderId, numbers.Generate(warehouseOrderId, now),
            sourceOrder.CustomerName, sourceOrder.RequestedDeliveryDate, actor.Id, actor.DisplayName, now,
            subiektSourceDocumentId: sourceOrder.SourceDocumentId, subiektSourceDocumentNumber: sourceOrder.Number);
        foreach (var sourceItem in sourceOrder.Items)
        {
            var product = await products.GetProductWarehouseOrderSnapshotAsync(sourceItem.ProductId, ct)
                ?? throw new RequestValidationException($"Source product {sourceItem.ProductId} is unavailable.");
            warehouseOrder.AddSubiektSourceItem(new WarehouseOrderSubiektSourceItem(sourceItem.SourceItemId), product.Id,
                product.Name, product.Symbol, sourceItem.Quantity, product.Unit, product.UnitWeightKg,
                actor.Id, actor.DisplayName, now);
        }

        var result = await warehouseOrders.AddAsync(warehouseOrder,
            audits.Create(actor, "WarehouseOrderCreatedFromSubiektCustomerOrder", "WarehouseOrder", warehouseOrder.Id, now), ct);
        if (result == WarehouseOrderStoreResult.Success)
            return new SubiektCustomerOrderConversionDto(warehouseOrder.Id, warehouseOrder.Number, false);

        existing = await warehouseOrders.FindBySubiektSourceDocumentIdAsync(sourceOrder.SourceDocumentId, ct);
        if (existing is not null)
            return new SubiektCustomerOrderConversionDto(existing.Value.Id, existing.Value.Number, true);
        throw new ResourceConflictException("The source order could not be converted.");
    }

    private async Task<SubiektCustomerOrderDto> FindSource(int sourceDocumentId, CancellationToken ct)
    {
        if (sourceDocumentId <= 0) throw new RequestValidationException("Source document identifier is invalid.");
        return await source.FindAsync(sourceDocumentId, ct)
            ?? throw new ResourceNotFoundException("Subiekt customer order was not found.");
    }
}
