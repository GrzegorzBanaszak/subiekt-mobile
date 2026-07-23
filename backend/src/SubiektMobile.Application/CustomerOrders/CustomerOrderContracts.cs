using SubiektMobile.Application.Customers;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.CustomerOrders;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Application.CustomerOrders;

public sealed record CustomerOrderListItemDto(Guid Id, string? CustomerOrderNumber, string CustomerName,
    string CustomerSiteName, DateOnly RequestedDeliveryDate, CustomerOrderStatus Status, int ItemCount,
    DateTimeOffset UpdatedAtUtc, long Version);

public sealed record CustomerOrderItemDto(Guid Id, string CustomerPartNumber, decimal Quantity);

public sealed record CustomerOrderDto(Guid Id, Guid CustomerId, string CustomerName, Guid CustomerSiteId,
    string CustomerSiteName, string? CustomerOrderNumber, string? DeliveryNoteNumber,
    DateOnly RequestedDeliveryDate, string? CustomerNotes, CustomerOrderStatus Status,
    Guid CreatedById, string CreatedByName, DateTimeOffset CreatedAtUtc, Guid UpdatedById, string UpdatedByName,
    DateTimeOffset UpdatedAtUtc, long Version, Guid? WarehouseOrderId,
    IReadOnlyList<CustomerOrderItemDto> Items);

public enum CustomerOrderReadinessSeverity { Blocking, Warning }

public sealed record CustomerOrderReadinessIssueDto(CustomerOrderReadinessSeverity Severity, string Code,
    Guid? CustomerOrderItemId, string Message);

public sealed record CustomerOrderItemResolutionDto(Guid CustomerOrderItemId, string CustomerPartNumber,
    CustomerPartReadiness Readiness, int? ProductId, string? ProductName, string? ProductSymbol,
    string? EngineeringChange, Guid? DefaultPackagingTypeId, string? CustomerPackagingCode);

public sealed record CustomerOrderReadinessDto(bool CanConvert,
    IReadOnlyList<CustomerOrderReadinessIssueDto> Issues,
    IReadOnlyList<CustomerOrderItemResolutionDto> Items);

public sealed record CustomerOrderActivityDto(Guid Id, string Action, ActorKind ActorKind,
    string ActorDisplayName, DateTimeOffset OccurredAtUtc);

public sealed record CustomerOrderConversionDto(CustomerOrderDto CustomerOrder, Guid WarehouseOrderId,
    string WarehouseOrderNumber);

public enum CustomerOrderStoreResult { Success, NotFound, Conflict }

public interface ICustomerOrderStore
{
    Task<PagedResult<CustomerOrderListItemDto>> ListAsync(string? search, CustomerOrderStatus? status,
        Guid? customerId, Guid? customerSiteId, DateOnly? dueDateFrom, DateOnly? dueDateTo,
        int page, int pageSize, CancellationToken cancellationToken);
    Task<CustomerOrder?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<Guid?> FindWarehouseOrderIdAsync(Guid customerOrderId, CancellationToken cancellationToken);
    Task<PagedResult<CustomerOrderActivityDto>> ListActivityAsync(Guid customerOrderId, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<CustomerOrderStoreResult> AddAsync(CustomerOrder order, AuditEntry audit, CancellationToken cancellationToken);
    Task<CustomerOrderStoreResult> SaveAsync(CustomerOrder order, long expectedVersion, AuditEntry audit,
        CancellationToken cancellationToken);
    Task<CustomerOrderStoreResult> ConvertAsync(CustomerOrder order, WarehouseOrder warehouseOrder,
        long expectedOrderVersion, AuditEntry customerOrderAudit, AuditEntry warehouseOrderAudit,
        CancellationToken cancellationToken);
}
