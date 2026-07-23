using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Application.WarehouseOrders;

public sealed record WarehouseOrderItemDto(Guid Id, int ProductId, string ProductName, string? ProductSymbol,
    decimal Quantity, string Unit, decimal? UnitWeightKg, WarehouseOrderItemStatus Status,
    Guid? CustomerOrderItemId, string? CustomerPartNumber, string? EngineeringChange,
    Guid? DefaultPackagingTypeId, string? CustomerPackagingCode, int? SubiektSourceItemId);
public sealed record WarehouseOrderAssigneeDto(Guid EmployeeId, Guid OrganizationId, string EmployeeDisplayName,
    Guid AssignedById, string AssignedByName, DateTimeOffset AssignedAtUtc);

public sealed record WarehouseOrderDto(Guid Id, string Number, string CustomerName, DateOnly DueDate,
    WarehouseOrderStatus Status, Guid CreatedById, string CreatedByName, DateTimeOffset CreatedAtUtc,
    Guid UpdatedById, string UpdatedByName, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc, long Version, PickingMode PickingMode, Guid? CustomerOrderId,
    string? CustomerDeliveryNoteNumber, int? SubiektSourceDocumentId, string? SubiektSourceDocumentNumber,
    IReadOnlyList<WarehouseOrderAssigneeDto> Assignees, IReadOnlyList<WarehouseOrderItemDto> Items);

public sealed record WarehouseOrderListItemDto(Guid Id, string Number, string CustomerName, DateOnly DueDate,
    WarehouseOrderStatus Status, int ItemCount, DateTimeOffset UpdatedAtUtc, long Version);

public sealed record ProductWarehouseOrderSnapshot(int Id, string Name, string? Symbol, string Unit, decimal? UnitWeightKg);
public sealed record AvailableWarehouseOrderAssigneeDto(Guid EmployeeId, Guid OrganizationId,
    string EmployeeDisplayName, string OrganizationName);

public enum WarehouseOrderStoreResult { Success, NotFound, Conflict }

public interface IWarehouseOrderStore
{
    Task<PagedResult<WarehouseOrderListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<WarehouseOrder?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<(Guid Id, string Number)?> FindBySubiektSourceDocumentIdAsync(int sourceDocumentId, CancellationToken cancellationToken) =>
        Task.FromResult<(Guid Id, string Number)?>(null);
    Task<IReadOnlyDictionary<int, Guid>> FindBySubiektSourceDocumentIdsAsync(IReadOnlyCollection<int> sourceDocumentIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, Guid>>(new Dictionary<int, Guid>());
    Task<WarehouseOrderStoreResult> AddAsync(WarehouseOrder warehouseOrder, AuditEntry audit, CancellationToken cancellationToken);
    Task<WarehouseOrderStoreResult> SaveAsync(WarehouseOrder warehouseOrder, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken);
    Task<WarehouseOrderStoreResult> DeleteAsync(WarehouseOrder warehouseOrder, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken);
}

public interface IWarehouseOrderNumberGenerator
{
    string Generate(Guid warehouseOrderId, DateTimeOffset now);
}

public interface IWarehouseOrderWorkforceDirectory
{
    Task<IReadOnlyList<AvailableWarehouseOrderAssigneeDto>> ListAvailableAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WarehouseOrderAssigneeCandidate>> ResolveActiveAsync(
        IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken);
}
