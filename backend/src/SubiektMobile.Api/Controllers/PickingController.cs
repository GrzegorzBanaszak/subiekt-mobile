using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Picking;
using SubiektMobile.Application.Products;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Route("api/picking/warehouse-orders")]
public sealed class PickingController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.WarehouseOrdersReadPublished)]
    [ProducesResponseType(typeof(PagedResult<PickingWarehouseOrderListItemDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<PickingWarehouseOrderListItemDto>> List([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] PickingWarehouseOrderStatus? status = null, [FromQuery] DateOnly? dueDateFrom = null,
        [FromQuery] DateOnly? dueDateTo = null, [FromQuery] string? customer = null,
        CancellationToken ct = default) => sender.Send(new ListPickingWarehouseOrdersQuery(page, pageSize,
            search, status, dueDateFrom, dueDateTo, customer), ct);

    [HttpGet("{warehouseOrderId:guid}")]
    [Authorize(Policy = Permissions.WarehouseOrdersReadPublished)]
    [ProducesResponseType(typeof(PickingWarehouseOrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PickingWarehouseOrderDetailsDto> Get(Guid warehouseOrderId, CancellationToken ct) =>
        sender.Send(new GetPickingWarehouseOrderQuery(warehouseOrderId), ct);

    [HttpGet("{warehouseOrderId:guid}/history")]
    [Authorize(Policy = Permissions.WarehouseOrdersReadPublished)]
    [ProducesResponseType(typeof(PagedResult<PickingHistoryItemDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<PickingHistoryItemDto>> History(Guid warehouseOrderId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        sender.Send(new ListPickingHistoryQuery(warehouseOrderId, page, pageSize), ct);

    [HttpPost("{warehouseOrderId:guid}/items/{itemId:guid}/reserve")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public Task<PickingWarehouseOrderDetailsDto> Reserve(Guid warehouseOrderId, Guid itemId,
        PickingMutationRequest request, CancellationToken ct) => sender.Send(
            new ReservePickingItemCommand(warehouseOrderId, itemId, request.OperationId, request.ItemVersion), ct);

    [HttpPost("{warehouseOrderId:guid}/items/{itemId:guid}/release")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public Task<PickingWarehouseOrderDetailsDto> Release(Guid warehouseOrderId, Guid itemId,
        PickingMutationRequest request, CancellationToken ct) => sender.Send(
            new ReleasePickingItemCommand(warehouseOrderId, itemId, request.OperationId, request.ItemVersion), ct);

    [HttpPost("{warehouseOrderId:guid}/items/{itemId:guid}/pack")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public Task<PickingWarehouseOrderDetailsDto> Pack(Guid warehouseOrderId, Guid itemId,
        PackPickingItemRequest request, CancellationToken ct) => sender.Send(
            new PackPickingItemCommand(warehouseOrderId, itemId, request.OperationId, request.ItemVersion,
                request.PackedQuantity), ct);

    [HttpPost("{warehouseOrderId:guid}/items/{itemId:guid}/undo-pack")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public Task<PickingWarehouseOrderDetailsDto> UndoPack(Guid warehouseOrderId, Guid itemId,
        PickingMutationRequest request, CancellationToken ct) => sender.Send(
            new UndoPackedPickingItemCommand(warehouseOrderId, itemId, request.OperationId, request.ItemVersion), ct);
}

public sealed record PickingMutationRequest(Guid OperationId, long ItemVersion);
public sealed record PackPickingItemRequest(Guid OperationId, long ItemVersion, decimal PackedQuantity);
