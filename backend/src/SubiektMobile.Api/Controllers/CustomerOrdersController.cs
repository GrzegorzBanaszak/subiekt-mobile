using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.CustomerOrders;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.CustomerOrdersManage)]
[Route("api/customer-orders")]
public sealed class CustomerOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<SubiektCustomerOrderListItemDto>> List([FromQuery] string? search,
        [FromQuery] bool includeCompleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        sender.Send(new ListSubiektCustomerOrdersQuery(search, includeCompleted, page, pageSize), ct);

    [HttpGet("{sourceDocumentId:int}")]
    public Task<SubiektCustomerOrderDto> Get(int sourceDocumentId, CancellationToken ct) =>
        sender.Send(new GetSubiektCustomerOrderQuery(sourceDocumentId), ct);

    [HttpPost("{sourceDocumentId:int}/convert")]
    public Task<SubiektCustomerOrderConversionDto> Convert(int sourceDocumentId, CancellationToken ct) =>
        sender.Send(new ConvertSubiektCustomerOrderCommand(sourceDocumentId), ct);
}
