using SubiektMobile.Application.Products;
using SubiektMobile.Application.Identity;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;

namespace SubiektMobile.Application.Picking;

public enum PickingWarehouseOrderStatus
{
    Waiting,
    InProgress,
    Completed
}

public sealed record PickingWarehouseOrderListFilter(int Page, int PageSize, string? Search,
    PickingWarehouseOrderStatus? Status, DateOnly? DueDateFrom, DateOnly? DueDateTo, string? Customer);

public sealed record PickingWarehouseOrderListItemDto(Guid Id, string Number, string CustomerName,
    DateOnly DueDate, PickingMode PickingMode, PickingWarehouseOrderStatus PickingStatus,
    int TotalItemCount, int CompletedItemCount, int ProgressPercent, bool IsAssignedToCurrentUser);

public sealed record PickingActorDto(ActorKind Kind, Guid Id, string DisplayName, DateTimeOffset AtUtc);
public sealed record PickingItemActionsDto(bool CanReserve, bool CanRelease, bool CanPack, bool CanUndoPack);
public sealed record PickingPalletAssignmentDto(Guid PalletId, string PalletNumber, decimal Quantity);

public sealed record PickingItemDto(Guid Id, int ProductId, string ProductName, string? ProductSymbol,
    decimal OrderedQuantity, decimal RemainingQuantity, string Unit, WarehouseOrderItemStatus Status, long Version,
    PickingActorDto? ReservedBy, decimal? PackedQuantity, PickingActorDto? PackedBy,
    decimal PalletizedQuantity, decimal AvailableForPalletQuantity,
    IReadOnlyList<PickingPalletAssignmentDto> PalletAssignments,
    PickingItemActionsDto Actions);

public sealed record PickingWarehouseOrderDetailsDto(Guid Id, string Number, string CustomerName,
    DateOnly DueDate, PickingMode PickingMode, PickingWarehouseOrderStatus PickingStatus,
    int TotalItemCount, int CompletedItemCount, int ProgressPercent,
    bool IsAssignedToCurrentUser, bool CanExecutePicking, bool CanCreatePallet,
    IReadOnlyList<PickingItemDto> Items);

public sealed record PickingHistoryItemDto(Guid Id, Guid OperationId, Guid WarehouseOrderItemId,
    string ProductName, PickingAction Action, WarehouseOrderItemStatus FromStatus, WarehouseOrderItemStatus ToStatus,
    decimal? PackedQuantity, ActorKind ActorKind, Guid ActorId, string ActorDisplayName,
    DateTimeOffset OccurredAtUtc);

public enum PickingStoreMutationResult { Success, Conflict, DuplicateOperation }

public interface IPickingStore
{
    Task<PagedResult<PickingWarehouseOrderListItemDto>> ListAsync(PickingWarehouseOrderListFilter filter,
        CurrentActor actor, CancellationToken cancellationToken);
    Task<WarehouseOrder?> FindWarehouseOrderAsync(Guid warehouseOrderId, bool tracking, CancellationToken cancellationToken);
    Task<PagedResult<PickingHistoryItemDto>> ListHistoryAsync(Guid warehouseOrderId, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PickingPalletAssignmentDto>>> ListPalletAssignmentsAsync(
        Guid warehouseOrderId, CancellationToken cancellationToken);
    Task<decimal> GetPalletizedQuantityAsync(Guid warehouseOrderItemId, CancellationToken cancellationToken);
    Task<WarehouseOrderPickingEvent?> FindOperationAsync(Guid operationId, CancellationToken cancellationToken);
    Task<PickingStoreMutationResult> SaveMutationAsync(WarehouseOrderItem item, long expectedVersion,
        WarehouseOrderPickingEvent pickingEvent, AuditEntry audit, CancellationToken cancellationToken);
}
