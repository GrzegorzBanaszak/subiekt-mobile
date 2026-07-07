using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Domain.Orders;

public enum PickingAction
{
    Reserved,
    Released,
    Packed,
    PackingUndone
}

public sealed class OrderPickingEvent
{
    private OrderPickingEvent() { }

    private OrderPickingEvent(Guid id, Guid operationId, Guid orderId, Guid orderItemId,
        string productName, PickingAction action, OrderItemStatus fromStatus, OrderItemStatus toStatus,
        decimal? packedQuantity, ActorKind actorKind, Guid actorId, string actorDisplayName,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty || operationId == Guid.Empty || orderId == Guid.Empty ||
            orderItemId == Guid.Empty || actorId == Guid.Empty)
            throw new ArgumentException("Picking event identifiers are required.");
        Id = id;
        OperationId = operationId;
        OrderId = orderId;
        OrderItemId = orderItemId;
        ProductName = Order.RequireText(productName, nameof(productName), 300);
        Action = action;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        PackedQuantity = packedQuantity;
        ActorKind = actorKind;
        ActorId = actorId;
        ActorDisplayName = Order.RequireText(actorDisplayName, nameof(actorDisplayName), 120);
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OperationId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public PickingAction Action { get; private set; }
    public OrderItemStatus FromStatus { get; private set; }
    public OrderItemStatus ToStatus { get; private set; }
    public decimal? PackedQuantity { get; private set; }
    public ActorKind ActorKind { get; private set; }
    public Guid ActorId { get; private set; }
    public string ActorDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static OrderPickingEvent Create(Guid operationId, Order order, OrderItem item,
        PickingAction action, OrderItemStatus fromStatus, decimal? packedQuantity,
        PickingActor actor, DateTimeOffset now) =>
        new(Guid.NewGuid(), operationId, order.Id, item.Id, item.ProductName, action, fromStatus,
            item.Status, packedQuantity, actor.Kind, actor.Id, actor.DisplayName, now);
}
