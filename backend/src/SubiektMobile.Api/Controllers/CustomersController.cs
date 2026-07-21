using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Customers;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Customers;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.CustomersManage)]
[Route("api/customers")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CustomerListItemDto>> List([FromQuery] string? search, [FromQuery] bool? isActive,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        sender.Send(new ListCustomersQuery(search, isActive, page, pageSize), ct);

    [HttpGet("{customerId:guid}")]
    public Task<CustomerDto> Get(Guid customerId, CancellationToken ct) =>
        sender.Send(new GetCustomerQuery(customerId), ct);

    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var customer = await sender.Send(new CreateCustomerCommand(request.Code, request.Name, request.TaxId,
            request.SubiektContractorId, request.InternalNotes, request.IsActive), ct);
        return CreatedAtAction(nameof(Get), new { customerId = customer.Id }, customer);
    }

    [HttpPut("{customerId:guid}")]
    public Task<CustomerDto> Update(Guid customerId, UpdateCustomerRequest request, CancellationToken ct) =>
        sender.Send(new UpdateCustomerCommand(customerId, request.Code, request.Name, request.TaxId,
            request.SubiektContractorId, request.InternalNotes, request.Version), ct);

    [HttpPut("{customerId:guid}/active")]
    public Task<CustomerDto> SetActive(Guid customerId, SetCustomerActiveRequest request, CancellationToken ct) =>
        sender.Send(new SetCustomerActiveCommand(customerId, request.IsActive, request.Version), ct);

    [HttpGet("{customerId:guid}/sites")]
    public Task<PagedResult<CustomerSiteListItemDto>> ListSites(Guid customerId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        sender.Send(new ListCustomerSitesQuery(customerId, search, page, pageSize), ct);

    [HttpGet("{customerId:guid}/sites/{siteId:guid}")]
    public Task<CustomerSiteDto> GetSite(Guid customerId, Guid siteId, CancellationToken ct) =>
        sender.Send(new GetCustomerSiteQuery(customerId, siteId), ct);

    [HttpPost("{customerId:guid}/sites")]
    [ProducesResponseType(typeof(CustomerSiteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerSiteDto>> CreateSite(Guid customerId, CreateCustomerSiteRequest request,
        CancellationToken ct)
    {
        var site = await sender.Send(new CreateCustomerSiteCommand(customerId, request.Code, request.Name,
            request.CountryCode, request.IsActive, request.CustomerVersion), ct);
        return CreatedAtAction(nameof(GetSite), new { customerId, siteId = site.Id }, site);
    }

    [HttpPut("{customerId:guid}/sites/{siteId:guid}")]
    public Task<CustomerSiteDto> UpdateSite(Guid customerId, Guid siteId, UpdateCustomerSiteRequest request,
        CancellationToken ct) => sender.Send(new UpdateCustomerSiteCommand(customerId, siteId, request.Code,
            request.Name, request.CountryCode, request.Version), ct);

    [HttpPut("{customerId:guid}/sites/{siteId:guid}/active")]
    public Task<CustomerSiteDto> SetSiteActive(Guid customerId, Guid siteId, SetCustomerSiteActiveRequest request,
        CancellationToken ct) => sender.Send(new SetCustomerSiteActiveCommand(customerId, siteId, request.IsActive,
            request.Version), ct);

    [HttpPut("{customerId:guid}/sites/{siteId:guid}/logistics-profile")]
    public Task<CustomerSiteDto> ConfigureLogisticsProfile(Guid customerId, Guid siteId,
        ConfigureCustomerSiteLogisticsProfileRequest request, CancellationToken ct) => sender.Send(
            new ConfigureCustomerSiteLogisticsProfileCommand(customerId, siteId,
                new CustomerSiteProfileInput(request.RecipientName, request.Street, request.PostalCode, request.City,
                    request.DefaultDock, request.ReceivingHours, request.SupplierNumber, request.DefaultPalletType,
                    request.MaximumPalletHeightCm, request.RequiresStretchFilm, request.RequiresStraps,
                    request.RequiresCornerProtectors, request.LoadSecuringNotes, request.LabelProfile), request.Version), ct);

    [HttpGet("{customerId:guid}/activity")]
    public Task<PagedResult<CustomerActivityDto>> ListActivity(Guid customerId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        sender.Send(new ListCustomerActivityQuery(customerId, page, pageSize), ct);
}

[ApiController]
[Authorize(Policy = Permissions.CustomersManage)]
[Route("api/customer-contractors")]
public sealed class CustomerContractorsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CustomerContractorDto>> Search([FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        sender.Send(new SearchCustomerContractorsQuery(search, page, pageSize), ct);
}

public sealed record CreateCustomerRequest(string Code, string Name, string? TaxId, int? SubiektContractorId,
    string? InternalNotes, bool IsActive = true);
public sealed record UpdateCustomerRequest(string Code, string Name, string? TaxId, int? SubiektContractorId,
    string? InternalNotes, long Version);
public sealed record SetCustomerActiveRequest(bool IsActive, long Version);
public sealed record CreateCustomerSiteRequest(string Code, string Name, string CountryCode, bool IsActive,
    long CustomerVersion);
public sealed record UpdateCustomerSiteRequest(string Code, string Name, string CountryCode, long Version);
public sealed record SetCustomerSiteActiveRequest(bool IsActive, long Version);
public sealed record ConfigureCustomerSiteLogisticsProfileRequest(string? RecipientName, string? Street,
    string? PostalCode, string? City, string? DefaultDock, string? ReceivingHours, string? SupplierNumber,
    string? DefaultPalletType, decimal? MaximumPalletHeightCm, bool RequiresStretchFilm, bool RequiresStraps,
    bool RequiresCornerProtectors, string? LoadSecuringNotes, VdaLabelProfile? LabelProfile, long Version);
