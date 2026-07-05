using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Orders;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Orders;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.OrdersManage)]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderListItemDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<OrderListItemDto>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) => sender.Send(new ListOrdersQuery(page, pageSize), ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrderDto> Get(Guid id, CancellationToken ct) => sender.Send(new GetOrderQuery(id), ct);

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await sender.Send(new CreateOrderCommand(request.CustomerName, request.DueDate,
            request.PickingMode, request.EmployeeIds), ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPut("{id:guid}")]
    public Task<OrderDto> Update(Guid id, UpdateOrderRequest request, CancellationToken ct) =>
        sender.Send(new UpdateOrderCommand(id, request.CustomerName, request.DueDate, request.Version), ct);

    [HttpPost("{id:guid}/items")]
    public Task<OrderDto> AddItem(Guid id, AddOrderItemRequest request, CancellationToken ct) =>
        sender.Send(new AddOrderItemCommand(id, request.ProductId, request.Quantity, request.Version), ct);

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public Task<OrderDto> RemoveItem(Guid id, Guid itemId, [FromQuery] long version, CancellationToken ct) =>
        sender.Send(new RemoveOrderItemCommand(id, itemId, version), ct);

    [HttpPost("{id:guid}/publish")]
    public Task<OrderDto> Publish(Guid id, VersionRequest request, CancellationToken ct) =>
        sender.Send(new PublishOrderCommand(id, request.Version), ct);

    [HttpGet("available-assignees")]
    public Task<IReadOnlyList<AvailableOrderAssigneeDto>> ListAvailableAssignees(CancellationToken ct) =>
        sender.Send(new ListAvailableOrderAssigneesQuery(), ct);

    [HttpPut("{id:guid}/picking-configuration")]
    public Task<OrderDto> ConfigurePicking(Guid id, ConfigureOrderPickingRequest request, CancellationToken ct) =>
        sender.Send(new ConfigureOrderPickingCommand(id, request.PickingMode, request.EmployeeIds,
            request.Version), ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] long version, CancellationToken ct)
    {
        await sender.Send(new DeleteOrderCommand(id, version), ct);
        return NoContent();
    }
}

public sealed record CreateOrderRequest(string CustomerName, DateOnly DueDate,
    PickingMode PickingMode, IReadOnlyCollection<Guid> EmployeeIds);
public sealed record UpdateOrderRequest(string CustomerName, DateOnly DueDate, long Version);
public sealed record AddOrderItemRequest(int ProductId, decimal Quantity, long Version);
public sealed record VersionRequest(long Version);
public sealed record ConfigureOrderPickingRequest(PickingMode PickingMode,
    IReadOnlyCollection<Guid> EmployeeIds, long Version);
