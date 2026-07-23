using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Customers;
using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Application.Customers;

public sealed record PackagingTypeDto(Guid Id, string Code, string Name, decimal TareWeightKg, decimal? DefaultCapacity, bool IsActive, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record CustomerPackagingCodeDto(Guid Id, Guid CustomerId, Guid? CustomerSiteId, Guid PackagingTypeId, string PackagingTypeCode, string PackagingTypeName, string Code, bool IsActive, long Version);
public sealed record CustomerPartMappingDto(Guid Id, Guid CustomerId, Guid? CustomerSiteId, string CustomerPartNumber, int ProductId, string? ProductSymbol, string? ProductName, Guid? DefaultPackagingTypeId, string? EngineeringChange, bool IsActive, long Version);
public enum CustomerPartReadiness { Mapped, MissingMapping, ProductUnavailable, PackagingUnavailable }
public sealed record CustomerPartResolutionDto(string CustomerPartNumber, CustomerPartReadiness Readiness, CustomerPartMappingDto? Mapping, CustomerPackagingCodeDto? PackagingCode);
public enum PackagingStoreResult { Success, NotFound, Conflict }

public interface IPackagingStore
{
    Task<PagedResult<PackagingTypeDto>> ListPackagingTypesAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct);
    Task<PackagingType?> FindPackagingTypeAsync(Guid id, bool tracking, CancellationToken ct);
    Task<PackagingStoreResult> AddPackagingTypeAsync(PackagingType entity, AuditEntry audit, CancellationToken ct);
    Task<PackagingStoreResult> SavePackagingTypeAsync(PackagingType entity, long version, AuditEntry audit, CancellationToken ct);
    Task<PagedResult<CustomerPackagingCodeDto>> ListPackagingCodesAsync(Guid customerId, Guid? siteId, int page, int pageSize, CancellationToken ct);
    Task<CustomerPackagingCode?> FindPackagingCodeAsync(Guid customerId, Guid? siteId, Guid id, bool tracking, CancellationToken ct);
    Task<PackagingStoreResult> AddPackagingCodeAsync(CustomerPackagingCode entity, AuditEntry audit, CancellationToken ct);
    Task<PackagingStoreResult> SavePackagingCodeAsync(CustomerPackagingCode entity, long version, AuditEntry audit, CancellationToken ct);
    Task<PagedResult<CustomerPartMappingDto>> ListPartMappingsAsync(Guid customerId, Guid? siteId, string? search, int page, int pageSize, CancellationToken ct);
    Task<CustomerPartMapping?> FindPartMappingAsync(Guid customerId, Guid? siteId, Guid id, bool tracking, CancellationToken ct);
    Task<PackagingStoreResult> AddPartMappingAsync(CustomerPartMapping entity, AuditEntry audit, CancellationToken ct);
    Task<PackagingStoreResult> SavePartMappingAsync(CustomerPartMapping entity, long version, AuditEntry audit, CancellationToken ct);
    Task<CustomerPartResolutionDto> ResolveAsync(Guid customerId, Guid? siteId, string partNumber, CancellationToken ct);
}
