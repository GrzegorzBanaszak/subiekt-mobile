using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Domain.WarehouseOrders;

public enum PickingAction
{
    Reserved,
    Released,
    Packed,
    PackingUndone
}

public sealed class WarehouseOrderPickingEvent
{
    private WarehouseOrderPickingEvent() { }

    private WarehouseOrderPickingEvent(Guid id, Guid operationId, Guid warehouseOrderId, Guid warehouseOrderItemId,
        string productName, PickingAction action, WarehouseOrderItemStatus fromStatus, WarehouseOrderItemStatus toStatus,
        decimal? packedQuantity, ActorKind actorKind, Guid actorId, string actorDisplayName,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty || operationId == Guid.Empty || warehouseOrderId == Guid.Empty ||
            warehouseOrderItemId == Guid.Empty || actorId == Guid.Empty)
            throw new ArgumentException("Picking event identifiers are required.");
        Id = id;
        OperationId = operationId;
        WarehouseOrderId = warehouseOrderId;
        WarehouseOrderItemId = warehouseOrderItemId;
        ProductName = WarehouseOrder.RequireText(productName, nameof(productName), 300);
        Action = action;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        PackedQuantity = packedQuantity;
        ActorKind = actorKind;
        ActorId = actorId;
        ActorDisplayName = WarehouseOrder.RequireText(actorDisplayName, nameof(actorDisplayName), 120);
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OperationId { get; private set; }
    public Guid WarehouseOrderId { get; private set; }
    public Guid WarehouseOrderItemId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public PickingAction Action { get; private set; }
    public WarehouseOrderItemStatus FromStatus { get; private set; }
    public WarehouseOrderItemStatus ToStatus { get; private set; }
    public decimal? PackedQuantity { get; private set; }
    public ActorKind ActorKind { get; private set; }
    public Guid ActorId { get; private set; }
    public string ActorDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static WarehouseOrderPickingEvent Create(Guid operationId, WarehouseOrder warehouseOrder, WarehouseOrderItem item,
        PickingAction action, WarehouseOrderItemStatus fromStatus, decimal? packedQuantity,
        PickingActor actor, DateTimeOffset now) =>
        new(Guid.NewGuid(), operationId, warehouseOrder.Id, item.Id, item.ProductName, action, fromStatus,
            item.Status, packedQuantity, actor.Kind, actor.Id, actor.DisplayName, now);
}
