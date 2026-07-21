using MediatR;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Customers;
using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Application.Customers;

public sealed record ListCustomersQuery(string? Search, bool? IsActive, int Page, int PageSize)
    : IRequest<PagedResult<CustomerListItemDto>>;
public sealed record GetCustomerQuery(Guid CustomerId) : IRequest<CustomerDto>;
public sealed record CreateCustomerCommand(string Code, string Name, string? TaxId, int? SubiektContractorId,
    string? InternalNotes, bool IsActive) : IRequest<CustomerDto>;
public sealed record UpdateCustomerCommand(Guid CustomerId, string Code, string Name, string? TaxId,
    int? SubiektContractorId, string? InternalNotes, long Version) : IRequest<CustomerDto>;
public sealed record SetCustomerActiveCommand(Guid CustomerId, bool IsActive, long Version) : IRequest<CustomerDto>;
public sealed record ListCustomerSitesQuery(Guid CustomerId, string? Search, int Page, int PageSize)
    : IRequest<PagedResult<CustomerSiteListItemDto>>;
public sealed record GetCustomerSiteQuery(Guid CustomerId, Guid SiteId) : IRequest<CustomerSiteDto>;
public sealed record CreateCustomerSiteCommand(Guid CustomerId, string Code, string Name, string CountryCode,
    bool IsActive, long CustomerVersion) : IRequest<CustomerSiteDto>;
public sealed record UpdateCustomerSiteCommand(Guid CustomerId, Guid SiteId, string Code, string Name,
    string CountryCode, long Version) : IRequest<CustomerSiteDto>;
public sealed record SetCustomerSiteActiveCommand(Guid CustomerId, Guid SiteId, bool IsActive, long Version)
    : IRequest<CustomerSiteDto>;
public sealed record ConfigureCustomerSiteLogisticsProfileCommand(Guid CustomerId, Guid SiteId,
    CustomerSiteProfileInput Profile, long Version) : IRequest<CustomerSiteDto>;
public sealed record ListCustomerActivityQuery(Guid CustomerId, int Page, int PageSize)
    : IRequest<PagedResult<CustomerActivityDto>>;
public sealed record SearchCustomerContractorsQuery(string? Search, int Page, int PageSize)
    : IRequest<PagedResult<CustomerContractorDto>>;

public sealed class ListCustomersHandler(ICustomerStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<ListCustomersQuery, PagedResult<CustomerListItemDto>>
{
    public Task<PagedResult<CustomerListItemDto>> Handle(ListCustomersQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomersManage);
        CustomerUseCases.ValidatePage(request.Page, request.PageSize);
        return store.ListAsync(request.Search, request.IsActive, request.Page, request.PageSize, ct);
    }
}

public sealed class GetCustomerHandler(ICustomerStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<GetCustomerQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomersManage);
        return CustomerUseCases.Map(await CustomerUseCases.FindCustomer(request.CustomerId, store, false, ct));
    }
}

public sealed class CreateCustomerHandler(ICustomerStore store, ICustomerContractorDirectory contractors,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomersManage);
        await CustomerUseCases.EnsureContractorExists(request.SubiektContractorId, contractors, ct);
        var now = time.GetUtcNow();
        try
        {
            var customer = Customer.Create(Guid.NewGuid(), request.Code, request.Name, request.TaxId,
                request.SubiektContractorId, request.InternalNotes, request.IsActive, now);
            var result = await store.AddAsync(customer,
                audits.Create(actor, "CustomerCreated", "Customer", customer.Id, now), ct);
            CustomerUseCases.EnsureSaved(result, "Customer could not be created.");
            return CustomerUseCases.Map(customer);
        }
        catch (ArgumentException ex) { throw new RequestValidationException(ex.Message); }
    }
}

public sealed class UpdateCustomerHandler(ICustomerStore store, ICustomerContractorDirectory contractors,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomersManage);
        var customer = await CustomerUseCases.FindCustomer(request.CustomerId, store, true, ct);
        CustomerUseCases.EnsureVersion(customer.Version, request.Version, "Customer");
        await CustomerUseCases.EnsureContractorExists(request.SubiektContractorId, contractors, ct);
        var now = time.GetUtcNow();
        try { customer.Update(request.Code, request.Name, request.TaxId, request.SubiektContractorId, request.InternalNotes, now); }
        catch (ArgumentException ex) { throw new RequestValidationException(ex.Message); }
        await CustomerUseCases.SaveCustomer(store, customer, request.Version,
            audits.Create(actor, "CustomerUpdated", "Customer", customer.Id, now), ct);
        return CustomerUseCases.Map(customer);
    }
}

public sealed class SetCustomerActiveHandler(ICustomerStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<SetCustomerActiveCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(SetCustomerActiveCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomersManage);
        var customer = await CustomerUseCases.FindCustomer(request.CustomerId, store, true, ct);
        CustomerUseCases.EnsureVersion(customer.Version, request.Version, "Customer");
        var now = time.GetUtcNow();
        customer.SetActive(request.IsActive, now);
        await CustomerUseCases.SaveCustomer(store, customer, request.Version,
            audits.Create(actor, request.IsActive ? "CustomerActivated" : "CustomerDeactivated", "Customer", customer.Id, now), ct);
        return CustomerUseCases.Map(customer);
    }
}

public sealed class ListCustomerSitesHandler(ICustomerStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<ListCustomerSitesQuery, PagedResult<CustomerSiteListItemDto>>
{
    public async Task<PagedResult<CustomerSiteListItemDto>> Handle(ListCustomerSitesQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomersManage);
        CustomerUseCases.ValidatePage(request.Page, request.PageSize);
        _ = await CustomerUseCases.FindCustomer(request.CustomerId, store, false, ct);
        return await store.ListSitesAsync(request.CustomerId, request.Search, request.Page, request.PageSize, ct);
    }
}

public sealed class GetCustomerSiteHandler(ICustomerStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<GetCustomerSiteQuery, CustomerSiteDto>
{
    public async Task<CustomerSiteDto> Handle(GetCustomerSiteQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomersManage);
        return CustomerUseCases.Map(await CustomerUseCases.FindSite(request.CustomerId, request.SiteId, store, false, ct));
    }
}

public sealed class CreateCustomerSiteHandler(ICustomerStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<CreateCustomerSiteCommand, CustomerSiteDto>
{
    public async Task<CustomerSiteDto> Handle(CreateCustomerSiteCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomersManage);
        var customer = await CustomerUseCases.FindCustomer(request.CustomerId, store, true, ct);
        CustomerUseCases.EnsureVersion(customer.Version, request.CustomerVersion, "Customer");
        var now = time.GetUtcNow();
        CustomerSite site;
        try { site = customer.AddSite(Guid.NewGuid(), request.Code, request.Name, request.CountryCode, request.IsActive, now); }
        catch (ArgumentException ex) { throw new RequestValidationException(ex.Message); }
        await CustomerUseCases.SaveCustomer(store, customer, request.CustomerVersion,
            audits.Create(actor, "CustomerSiteCreated", "CustomerSite", site.Id, now), ct);
        return CustomerUseCases.Map(site);
    }
}

public sealed class UpdateCustomerSiteHandler(ICustomerStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<UpdateCustomerSiteCommand, CustomerSiteDto>
{
    public async Task<CustomerSiteDto> Handle(UpdateCustomerSiteCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomersManage);
        var site = await CustomerUseCases.FindSite(request.CustomerId, request.SiteId, store, true, ct);
        CustomerUseCases.EnsureVersion(site.Version, request.Version, "Customer site");
        var now = time.GetUtcNow();
        try { site.Update(request.Code, request.Name, request.CountryCode, now); }
        catch (ArgumentException ex) { throw new RequestValidationException(ex.Message); }
        await CustomerUseCases.SaveSite(store, site, request.Version,
            audits.Create(actor, "CustomerSiteUpdated", "CustomerSite", site.Id, now), ct);
        return CustomerUseCases.Map(site);
    }
}

public sealed class SetCustomerSiteActiveHandler(ICustomerStore store, IApplicationAuthorizationService authorization,
    IAuditEntryFactory audits, TimeProvider time) : IRequestHandler<SetCustomerSiteActiveCommand, CustomerSiteDto>
{
    public async Task<CustomerSiteDto> Handle(SetCustomerSiteActiveCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomersManage);
        var site = await CustomerUseCases.FindSite(request.CustomerId, request.SiteId, store, true, ct);
        CustomerUseCases.EnsureVersion(site.Version, request.Version, "Customer site");
        var now = time.GetUtcNow();
        site.SetActive(request.IsActive, now);
        await CustomerUseCases.SaveSite(store, site, request.Version,
            audits.Create(actor, request.IsActive ? "CustomerSiteActivated" : "CustomerSiteDeactivated", "CustomerSite", site.Id, now), ct);
        return CustomerUseCases.Map(site);
    }
}

public sealed class ConfigureCustomerSiteLogisticsProfileHandler(ICustomerStore store,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<ConfigureCustomerSiteLogisticsProfileCommand, CustomerSiteDto>
{
    public async Task<CustomerSiteDto> Handle(ConfigureCustomerSiteLogisticsProfileCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.CustomersManage);
        var site = await CustomerUseCases.FindSite(request.CustomerId, request.SiteId, store, true, ct);
        CustomerUseCases.EnsureVersion(site.Version, request.Version, "Customer site");
        var now = time.GetUtcNow();
        try { site.ConfigureLogisticsProfile(request.Profile, now); }
        catch (ArgumentException ex) { throw new RequestValidationException(ex.Message); }
        await CustomerUseCases.SaveSite(store, site, request.Version,
            audits.Create(actor, "CustomerSiteLogisticsProfileConfigured", "CustomerSite", site.Id, now), ct);
        return CustomerUseCases.Map(site);
    }
}

public sealed class ListCustomerActivityHandler(ICustomerStore store, IApplicationAuthorizationService authorization)
    : IRequestHandler<ListCustomerActivityQuery, PagedResult<CustomerActivityDto>>
{
    public async Task<PagedResult<CustomerActivityDto>> Handle(ListCustomerActivityQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomersManage);
        CustomerUseCases.ValidatePage(request.Page, request.PageSize);
        _ = await CustomerUseCases.FindCustomer(request.CustomerId, store, false, ct);
        return await store.ListActivityAsync(request.CustomerId, request.Page, request.PageSize, ct);
    }
}

public sealed class SearchCustomerContractorsHandler(ICustomerContractorDirectory contractors,
    IApplicationAuthorizationService authorization) : IRequestHandler<SearchCustomerContractorsQuery, PagedResult<CustomerContractorDto>>
{
    public Task<PagedResult<CustomerContractorDto>> Handle(SearchCustomerContractorsQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.CustomersManage);
        CustomerUseCases.ValidatePage(request.Page, request.PageSize);
        return contractors.SearchAsync(request.Search, request.Page, request.PageSize, ct);
    }
}

internal static class CustomerUseCases
{
    public static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new RequestValidationException("Page must be at least 1 and pageSize must be between 1 and 100.");
    }

    public static async Task<Customer> FindCustomer(Guid id, ICustomerStore store, bool tracking, CancellationToken ct) =>
        await store.FindAsync(id, tracking, ct) ?? throw new ResourceNotFoundException("Customer was not found.");

    public static async Task<CustomerSite> FindSite(Guid customerId, Guid siteId, ICustomerStore store, bool tracking,
        CancellationToken ct) => await store.FindSiteAsync(customerId, siteId, tracking, ct)
            ?? throw new ResourceNotFoundException("Customer site was not found.");

    public static async Task EnsureContractorExists(int? contractorId, ICustomerContractorDirectory contractors,
        CancellationToken ct)
    {
        if (contractorId is not null && !await contractors.ExistsAsync(contractorId.Value, ct))
            throw new RequestValidationException("The selected Subiekt contractor no longer exists or is blocked.");
    }

    public static void EnsureVersion(long actual, long expected, string resource)
    {
        if (expected <= 0 || actual != expected)
            throw new ResourceConflictException($"{resource} was modified by another request.");
    }

    public static void EnsureSaved(CustomerStoreResult result, string conflictMessage)
    {
        if (result == CustomerStoreResult.Conflict) throw new ResourceConflictException(conflictMessage);
        if (result == CustomerStoreResult.NotFound) throw new ResourceNotFoundException("Customer was not found.");
    }

    public static async Task SaveCustomer(ICustomerStore store, Customer customer, long expectedVersion, AuditEntry audit,
        CancellationToken ct) => EnsureSaved(await store.SaveCustomerAsync(customer, expectedVersion, audit, ct),
            "Customer was modified by another request.");

    public static async Task SaveSite(ICustomerStore store, CustomerSite site, long expectedVersion, AuditEntry audit,
        CancellationToken ct) => EnsureSaved(await store.SaveSiteAsync(site, expectedVersion, audit, ct),
            "Customer site was modified by another request.");

    public static CustomerDto Map(Customer customer) => new(customer.Id, customer.Code, customer.Name, customer.TaxId,
        customer.SubiektContractorId, customer.IsActive, customer.InternalNotes, customer.CreatedAtUtc, customer.UpdatedAtUtc,
        customer.Version, customer.Sites.OrderBy(x => x.Code).Select(MapListItem).ToList());

    public static CustomerSiteListItemDto MapListItem(CustomerSite site) => new(site.Id, site.Code, site.Name,
        site.CountryCode, site.IsActive, site.LogisticsProfile?.DefaultDock, site.LogisticsProfile?.SupplierNumber,
        site.LogisticsProfile?.LabelProfile, site.LogisticsProfile?.IsComplete == true, site.Version);

    public static CustomerSiteDto Map(CustomerSite site) => new(site.Id, site.CustomerId, site.Code, site.Name,
        site.CountryCode, site.IsActive, site.CreatedAtUtc, site.UpdatedAtUtc, site.Version,
        site.LogisticsProfile is null ? null : new CustomerSiteLogisticsProfileDto(
            site.LogisticsProfile.RecipientName, site.LogisticsProfile.Street, site.LogisticsProfile.PostalCode,
            site.LogisticsProfile.City, site.LogisticsProfile.DefaultDock, site.LogisticsProfile.ReceivingHours,
            site.LogisticsProfile.SupplierNumber, site.LogisticsProfile.DefaultPalletType,
            site.LogisticsProfile.MaximumPalletHeightCm, site.LogisticsProfile.RequiresStretchFilm,
            site.LogisticsProfile.RequiresStraps, site.LogisticsProfile.RequiresCornerProtectors,
            site.LogisticsProfile.LoadSecuringNotes, site.LogisticsProfile.LabelProfile,
            site.LogisticsProfile.IsComplete));
}
