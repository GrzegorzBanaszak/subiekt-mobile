using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Customers;
using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Application.Customers;

public sealed record CustomerListItemDto(Guid Id, string Code, string Name, string? TaxId, bool IsActive,
    int SiteCount, int CompleteProfileCount, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record CustomerContractorDto(int Id, string Symbol, string Name, string? TaxId);
public sealed record CustomerSiteLogisticsProfileDto(string? RecipientName, string? Street, string? PostalCode,
    string? City, string? DefaultDock, string? ReceivingHours, string? SupplierNumber, string? DefaultPalletType,
    decimal? MaximumPalletHeightCm, bool RequiresStretchFilm, bool RequiresStraps, bool RequiresCornerProtectors,
    string? LoadSecuringNotes, VdaLabelProfile? LabelProfile, bool IsComplete);
public sealed record CustomerSiteListItemDto(Guid Id, string Code, string Name, string CountryCode, bool IsActive,
    string? DefaultDock, string? SupplierNumber, VdaLabelProfile? LabelProfile, bool HasCompleteProfile,
    long Version);
public sealed record CustomerSiteDto(Guid Id, Guid CustomerId, string Code, string Name, string CountryCode,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version,
    CustomerSiteLogisticsProfileDto? LogisticsProfile);
public sealed record CustomerDto(Guid Id, string Code, string Name, string? TaxId, int? SubiektContractorId,
    bool IsActive, string? InternalNotes, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version,
    IReadOnlyList<CustomerSiteListItemDto> Sites);
public sealed record CustomerActivityDto(Guid Id, string Action, string TargetType, Guid? TargetId,
    ActorKind ActorKind, string ActorDisplayName, DateTimeOffset OccurredAtUtc);

public enum CustomerStoreResult { Success, NotFound, Conflict }

public interface ICustomerStore
{
    Task<PagedResult<CustomerListItemDto>> ListAsync(string? search, bool? isActive, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<Customer?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<CustomerSite?> FindSiteAsync(Guid customerId, Guid siteId, bool tracking, CancellationToken cancellationToken);
    Task<PagedResult<CustomerSiteListItemDto>> ListSitesAsync(Guid customerId, string? search, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<PagedResult<CustomerActivityDto>> ListActivityAsync(Guid customerId, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<CustomerStoreResult> AddAsync(Customer customer, AuditEntry audit, CancellationToken cancellationToken);
    Task<CustomerStoreResult> SaveCustomerAsync(Customer customer, long expectedVersion, AuditEntry audit,
        CancellationToken cancellationToken);
    Task<CustomerStoreResult> SaveSiteAsync(CustomerSite site, long expectedVersion, AuditEntry audit,
        CancellationToken cancellationToken);
}

public interface ICustomerContractorDirectory
{
    Task<PagedResult<CustomerContractorDto>> SearchAsync(string? search, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
}
