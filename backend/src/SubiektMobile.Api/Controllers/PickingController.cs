using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Picking;
using SubiektMobile.Application.Products;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Route("api/picking/orders")]
public sealed class PickingController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.OrdersReadPublished)]
    [ProducesResponseType(typeof(PagedResult<PickingOrderListItemDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<PickingOrderListItemDto>> List([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] PickingOrderStatus? status = null, [FromQuery] DateOnly? dueDateFrom = null,
        [FromQuery] DateOnly? dueDateTo = null, [FromQuery] string? customer = null,
        CancellationToken ct = default) => sender.Send(new ListPickingOrdersQuery(page, pageSize,
            search, status, dueDateFrom, dueDateTo, customer), ct);

    [HttpGet("{orderId:guid}")]
    [Authorize(Policy = Permissions.OrdersReadPublished)]
    [ProducesResponseType(typeof(PickingOrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PickingOrderDetailsDto> Get(Guid orderId, CancellationToken ct) =>
        sender.Send(new GetPickingOrderQuery(orderId), ct);

    [HttpGet("{orderId:guid}/history")]
    [Authorize(Policy = Permissions.OrdersReadPublished)]
    [ProducesResponseType(typeof(PagedResult<PickingHistoryItemDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<PickingHistoryItemDto>> History(Guid orderId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        sender.Send(new ListPickingHistoryQuery(orderId, page, pageSize), ct);

    [HttpPost("{orderId:guid}/items/{itemId:guid}/reserve")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public Task<PickingOrderDetailsDto> Reserve(Guid orderId, Guid itemId,
        PickingMutationRequest request, CancellationToken ct) => sender.Send(
            new ReservePickingItemCommand(orderId, itemId, request.OperationId, request.ItemVersion), ct);

    [HttpPost("{orderId:guid}/items/{itemId:guid}/release")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public Task<PickingOrderDetailsDto> Release(Guid orderId, Guid itemId,
        PickingMutationRequest request, CancellationToken ct) => sender.Send(
            new ReleasePickingItemCommand(orderId, itemId, request.OperationId, request.ItemVersion), ct);

    [HttpPost("{orderId:guid}/items/{itemId:guid}/pack")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public Task<PickingOrderDetailsDto> Pack(Guid orderId, Guid itemId,
        PackPickingItemRequest request, CancellationToken ct) => sender.Send(
            new PackPickingItemCommand(orderId, itemId, request.OperationId, request.ItemVersion,
                request.PackedQuantity), ct);

    [HttpPost("{orderId:guid}/items/{itemId:guid}/undo-pack")]
    [Authorize(Policy = Permissions.PickingExecute)]
    public Task<PickingOrderDetailsDto> UndoPack(Guid orderId, Guid itemId,
        PickingMutationRequest request, CancellationToken ct) => sender.Send(
            new UndoPackedPickingItemCommand(orderId, itemId, request.OperationId, request.ItemVersion), ct);
}

public sealed record PickingMutationRequest(Guid OperationId, long ItemVersion);
public sealed record PackPickingItemRequest(Guid OperationId, long ItemVersion, decimal PackedQuantity);
