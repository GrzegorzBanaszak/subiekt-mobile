using SubiektMobile.Application.Products;
using SubiektMobile.Application.Identity;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;

namespace SubiektMobile.Application.Picking;

public enum PickingOrderStatus
{
    Waiting,
    InProgress,
    Completed
}

public sealed record PickingOrderListFilter(int Page, int PageSize, string? Search,
    PickingOrderStatus? Status, DateOnly? DueDateFrom, DateOnly? DueDateTo, string? Customer);

public sealed record PickingOrderListItemDto(Guid Id, string Number, string CustomerName,
    DateOnly DueDate, PickingMode PickingMode, PickingOrderStatus PickingStatus,
    int TotalItemCount, int CompletedItemCount, int ProgressPercent, bool IsAssignedToCurrentUser);

public sealed record PickingActorDto(ActorKind Kind, Guid Id, string DisplayName, DateTimeOffset AtUtc);
public sealed record PickingItemActionsDto(bool CanReserve, bool CanRelease, bool CanPack, bool CanUndoPack);
public sealed record PickingPalletAssignmentDto(Guid PalletId, string PalletNumber, decimal Quantity);

public sealed record PickingItemDto(Guid Id, int ProductId, string ProductName, string? ProductSymbol,
    decimal OrderedQuantity, decimal RemainingQuantity, string Unit, OrderItemStatus Status, long Version,
    PickingActorDto? ReservedBy, decimal? PackedQuantity, PickingActorDto? PackedBy,
    decimal PalletizedQuantity, decimal AvailableForPalletQuantity,
    IReadOnlyList<PickingPalletAssignmentDto> PalletAssignments,
    PickingItemActionsDto Actions);

public sealed record PickingOrderDetailsDto(Guid Id, string Number, string CustomerName,
    DateOnly DueDate, PickingMode PickingMode, PickingOrderStatus PickingStatus,
    int TotalItemCount, int CompletedItemCount, int ProgressPercent,
    bool IsAssignedToCurrentUser, bool CanExecutePicking, bool CanCreatePallet,
    IReadOnlyList<PickingItemDto> Items);

public sealed record PickingHistoryItemDto(Guid Id, Guid OperationId, Guid OrderItemId,
    string ProductName, PickingAction Action, OrderItemStatus FromStatus, OrderItemStatus ToStatus,
    decimal? PackedQuantity, ActorKind ActorKind, Guid ActorId, string ActorDisplayName,
    DateTimeOffset OccurredAtUtc);

public enum PickingStoreMutationResult { Success, Conflict, DuplicateOperation }

public interface IPickingStore
{
    Task<PagedResult<PickingOrderListItemDto>> ListAsync(PickingOrderListFilter filter,
        CurrentActor actor, CancellationToken cancellationToken);
    Task<Order?> FindOrderAsync(Guid orderId, bool tracking, CancellationToken cancellationToken);
    Task<PagedResult<PickingHistoryItemDto>> ListHistoryAsync(Guid orderId, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PickingPalletAssignmentDto>>> ListPalletAssignmentsAsync(
        Guid orderId, CancellationToken cancellationToken);
    Task<decimal> GetPalletizedQuantityAsync(Guid orderItemId, CancellationToken cancellationToken);
    Task<OrderPickingEvent?> FindOperationAsync(Guid operationId, CancellationToken cancellationToken);
    Task<PickingStoreMutationResult> SaveMutationAsync(OrderItem item, long expectedVersion,
        OrderPickingEvent pickingEvent, AuditEntry audit, CancellationToken cancellationToken);
}
