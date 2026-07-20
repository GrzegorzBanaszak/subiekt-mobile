using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.WarehouseOrders;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.WarehouseOrdersManage)]
[Route("api/warehouse-orders")]
public sealed class WarehouseOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WarehouseOrderListItemDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<WarehouseOrderListItemDto>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) => sender.Send(new ListWarehouseOrdersQuery(page, pageSize), ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WarehouseOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WarehouseOrderDto> Get(Guid id, CancellationToken ct) => sender.Send(new GetWarehouseOrderQuery(id), ct);

    [HttpPost]
    [ProducesResponseType(typeof(WarehouseOrderDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WarehouseOrderDto>> Create(CreateWarehouseOrderRequest request, CancellationToken ct)
    {
        var warehouseOrder = await sender.Send(new CreateWarehouseOrderCommand(request.CustomerName, request.DueDate,
            request.PickingMode, request.EmployeeIds,
            request.Items?.Select(x => new CreateWarehouseOrderItemInput(x.ProductId, x.Quantity)).ToList() ?? []), ct);
        return CreatedAtAction(nameof(Get), new { id = warehouseOrder.Id }, warehouseOrder);
    }

    [HttpPut("{id:guid}")]
    public Task<WarehouseOrderDto> Update(Guid id, UpdateWarehouseOrderRequest request, CancellationToken ct) =>
        sender.Send(new UpdateWarehouseOrderCommand(id, request.CustomerName, request.DueDate, request.Version), ct);

    [HttpPost("{id:guid}/items")]
    public Task<WarehouseOrderDto> AddItem(Guid id, AddWarehouseOrderItemRequest request, CancellationToken ct) =>
        sender.Send(new AddWarehouseOrderItemCommand(id, request.ProductId, request.Quantity, request.Version), ct);

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public Task<WarehouseOrderDto> RemoveItem(Guid id, Guid itemId, [FromQuery] long version, CancellationToken ct) =>
        sender.Send(new RemoveWarehouseOrderItemCommand(id, itemId, version), ct);

    [HttpPost("{id:guid}/publish")]
    public Task<WarehouseOrderDto> Publish(Guid id, VersionRequest request, CancellationToken ct) =>
        sender.Send(new PublishWarehouseOrderCommand(id, request.Version), ct);

    [HttpGet("available-assignees")]
    public Task<IReadOnlyList<AvailableWarehouseOrderAssigneeDto>> ListAvailableAssignees(CancellationToken ct) =>
        sender.Send(new ListAvailableWarehouseOrderAssigneesQuery(), ct);

    [HttpPut("{id:guid}/picking-configuration")]
    public Task<WarehouseOrderDto> ConfigurePicking(Guid id, ConfigureWarehouseOrderPickingRequest request, CancellationToken ct) =>
        sender.Send(new ConfigureWarehouseOrderPickingCommand(id, request.PickingMode, request.EmployeeIds,
            request.Version), ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] long version, CancellationToken ct)
    {
        await sender.Send(new DeleteWarehouseOrderCommand(id, version), ct);
        return NoContent();
    }
}

public sealed record CreateWarehouseOrderRequest(string CustomerName, DateOnly DueDate,
    PickingMode PickingMode, IReadOnlyCollection<Guid> EmployeeIds,
    IReadOnlyCollection<CreateWarehouseOrderItemRequest>? Items = null);
public sealed record CreateWarehouseOrderItemRequest(int ProductId, decimal Quantity);
public sealed record UpdateWarehouseOrderRequest(string CustomerName, DateOnly DueDate, long Version);
public sealed record AddWarehouseOrderItemRequest(int ProductId, decimal Quantity, long Version);
public sealed record VersionRequest(long Version);
public sealed record ConfigureWarehouseOrderPickingRequest(PickingMode PickingMode,
    IReadOnlyCollection<Guid> EmployeeIds, long Version);
