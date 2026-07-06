using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;

namespace SubiektMobile.Application.Orders;

public sealed record OrderItemDto(Guid Id, int ProductId, string ProductName, string? ProductSymbol,
    decimal Quantity, string Unit, decimal? UnitWeightKg, OrderItemStatus Status);
public sealed record OrderAssigneeDto(Guid EmployeeId, Guid OrganizationId, string EmployeeDisplayName,
    Guid AssignedById, string AssignedByName, DateTimeOffset AssignedAtUtc);

public sealed record OrderDto(Guid Id, string Number, string CustomerName, DateOnly DueDate,
    OrderStatus Status, Guid CreatedById, string CreatedByName, DateTimeOffset CreatedAtUtc,
    Guid UpdatedById, string UpdatedByName, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc, long Version, PickingMode PickingMode,
    IReadOnlyList<OrderAssigneeDto> Assignees, IReadOnlyList<OrderItemDto> Items);

public sealed record OrderListItemDto(Guid Id, string Number, string CustomerName, DateOnly DueDate,
    OrderStatus Status, int ItemCount, DateTimeOffset UpdatedAtUtc, long Version);

public sealed record ProductOrderSnapshot(int Id, string Name, string? Symbol, string Unit, decimal? UnitWeightKg);
public sealed record AvailableOrderAssigneeDto(Guid EmployeeId, Guid OrganizationId,
    string EmployeeDisplayName, string OrganizationName);

public enum OrderStoreResult { Success, NotFound, Conflict }

public interface IOrderStore
{
    Task<PagedResult<OrderListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<Order?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<OrderStoreResult> AddAsync(Order order, AuditEntry audit, CancellationToken cancellationToken);
    Task<OrderStoreResult> SaveAsync(Order order, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken);
    Task<OrderStoreResult> DeleteAsync(Order order, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken);
}

public interface IOrderNumberGenerator
{
    string Generate(Guid orderId, DateTimeOffset now);
}

public interface IOrderWorkforceDirectory
{
    Task<IReadOnlyList<AvailableOrderAssigneeDto>> ListAvailableAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderAssigneeCandidate>> ResolveActiveAsync(
        IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken);
}
