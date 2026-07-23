using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Customers;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;

namespace SubiektMobile.Api.Controllers;

[ApiController, Authorize(Policy = Permissions.CustomersManage)]
[Route("api/packaging-types")]
public sealed class PackagingTypesController(ISender sender) : ControllerBase
{
    [HttpGet] public Task<PagedResult<PackagingTypeDto>> List([FromQuery] string? search, [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => sender.Send(new ListPackagingTypesQuery(search, isActive, page, pageSize), ct);
    [HttpPost] public async Task<ActionResult<PackagingTypeDto>> Create(PackagingTypeRequest r, CancellationToken ct) { var x = await sender.Send(new CreatePackagingTypeCommand(r.Code, r.Name, r.TareWeightKg, r.DefaultCapacity, r.IsActive), ct); return CreatedAtAction(nameof(List), x); }
    [HttpPut("{id:guid}")] public Task<PackagingTypeDto> Update(Guid id, PackagingTypeUpdateRequest r, CancellationToken ct) => sender.Send(new UpdatePackagingTypeCommand(id, r.Code, r.Name, r.TareWeightKg, r.DefaultCapacity, r.Version), ct);
    [HttpPut("{id:guid}/active")] public Task<PackagingTypeDto> Active(Guid id, ActiveRequest r, CancellationToken ct) => sender.Send(new SetPackagingTypeActiveCommand(id, r.IsActive, r.Version), ct);
}

[ApiController, Authorize(Policy = Permissions.CustomersManage)]
[Route("api/customers/{customerId:guid}/packaging")]
public sealed class CustomerPackagingController(ISender sender) : ControllerBase
{
    [HttpGet("codes")] public Task<PagedResult<CustomerPackagingCodeDto>> Codes(Guid customerId, [FromQuery] Guid? siteId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default) => sender.Send(new ListCustomerPackagingCodesQuery(customerId, siteId, page, pageSize), ct);
    [HttpPost("codes")] public Task<CustomerPackagingCodeDto> CreateCode(Guid customerId, CustomerPackagingCodeRequest r, CancellationToken ct) => sender.Send(new CreateCustomerPackagingCodeCommand(customerId, r.SiteId, r.PackagingTypeId, r.Code, r.IsActive), ct);
    [HttpPut("codes/{id:guid}")] public Task<CustomerPackagingCodeDto> UpdateCode(Guid customerId, Guid id, CustomerPackagingCodeUpdateRequest r, CancellationToken ct) => sender.Send(new UpdateCustomerPackagingCodeCommand(customerId, r.SiteId, id, r.PackagingTypeId, r.Code, r.Version), ct);
    [HttpPut("codes/{id:guid}/active")] public Task<CustomerPackagingCodeDto> ActiveCode(Guid customerId, Guid id, CustomerPackagingCodeActiveRequest r, CancellationToken ct) => sender.Send(new SetCustomerPackagingCodeActiveCommand(customerId, r.SiteId, id, r.IsActive, r.Version), ct);
    [HttpGet("parts")] public Task<PagedResult<CustomerPartMappingDto>> Parts(Guid customerId, [FromQuery] Guid? siteId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default) => sender.Send(new ListCustomerPartMappingsQuery(customerId, siteId, search, page, pageSize), ct);
    [HttpPost("parts")] public Task<CustomerPartMappingDto> CreatePart(Guid customerId, CustomerPartMappingRequest r, CancellationToken ct) => sender.Send(new CreateCustomerPartMappingCommand(customerId, r.SiteId, r.PartNumber, r.ProductId, r.DefaultPackagingTypeId, r.EngineeringChange, r.IsActive), ct);
    [HttpPut("parts/{id:guid}")] public Task<CustomerPartMappingDto> UpdatePart(Guid customerId, Guid id, CustomerPartMappingUpdateRequest r, CancellationToken ct) => sender.Send(new UpdateCustomerPartMappingCommand(customerId, r.SiteId, id, r.PartNumber, r.ProductId, r.DefaultPackagingTypeId, r.EngineeringChange, r.Version), ct);
    [HttpPut("parts/{id:guid}/active")] public Task<CustomerPartMappingDto> ActivePart(Guid customerId, Guid id, CustomerPartMappingActiveRequest r, CancellationToken ct) => sender.Send(new SetCustomerPartMappingActiveCommand(customerId, r.SiteId, id, r.IsActive, r.Version), ct);
    [HttpGet("parts/resolve")] public Task<CustomerPartResolutionDto> Resolve(Guid customerId, [FromQuery] Guid? siteId, [FromQuery] string partNumber, CancellationToken ct) => sender.Send(new ResolveCustomerPartQuery(customerId, siteId, partNumber), ct);
}

public sealed record PackagingTypeRequest(string Code, string Name, decimal TareWeightKg, decimal? DefaultCapacity, bool IsActive = true);
public sealed record PackagingTypeUpdateRequest(string Code, string Name, decimal TareWeightKg, decimal? DefaultCapacity, long Version);
public sealed record ActiveRequest(bool IsActive, long Version);
public sealed record CustomerPackagingCodeRequest(Guid? SiteId, Guid PackagingTypeId, string Code, bool IsActive = true);
public sealed record CustomerPackagingCodeUpdateRequest(Guid? SiteId, Guid PackagingTypeId, string Code, long Version);
public sealed record CustomerPackagingCodeActiveRequest(Guid? SiteId, bool IsActive, long Version);
public sealed record CustomerPartMappingRequest(Guid? SiteId, string PartNumber, int ProductId, Guid? DefaultPackagingTypeId, string? EngineeringChange, bool IsActive = true);
public sealed record CustomerPartMappingUpdateRequest(Guid? SiteId, string PartNumber, int ProductId, Guid? DefaultPackagingTypeId, string? EngineeringChange, long Version);
public sealed record CustomerPartMappingActiveRequest(Guid? SiteId, bool IsActive, long Version);
