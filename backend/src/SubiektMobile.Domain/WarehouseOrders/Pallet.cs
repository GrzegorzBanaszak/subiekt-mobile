using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Domain.WarehouseOrders;

public enum PalletStatus
{
    Closed
}

public sealed record PalletItemAllocation(
    Guid WarehouseOrderItemId,
    decimal Quantity,
    decimal UnitWeightKg);

public sealed class Pallet
{
    private readonly List<PalletItem> _items = [];

    private Pallet() { }

    private Pallet(Guid id, Guid operationId, Guid warehouseOrderId, string number,
        decimal emptyPalletWeightKg, IReadOnlyCollection<PalletItemAllocation> allocations,
        PickingActor actor, DateTimeOffset now)
    {
        if (id == Guid.Empty || operationId == Guid.Empty || warehouseOrderId == Guid.Empty)
            throw new ArgumentException("Pallet identifiers are required.");
        ValidateActor(actor);
        if (emptyPalletWeightKg < 0)
            throw new ArgumentOutOfRangeException(nameof(emptyPalletWeightKg), "Empty pallet weight cannot be negative.");
        if (allocations.Count == 0)
            throw new InvalidOperationException("Pallet must contain at least one item.");
        if (allocations.Select(x => x.WarehouseOrderItemId).Distinct().Count() != allocations.Count)
            throw new InvalidOperationException("An order item can be assigned to a pallet only once in a single operation.");

        Id = id;
        OperationId = operationId;
        WarehouseOrderId = warehouseOrderId;
        Number = WarehouseOrder.RequireText(number, nameof(number), 40);
        Status = PalletStatus.Closed;
        EmptyPalletWeightKg = NormalizeWeight(emptyPalletWeightKg);
        ClosedByKind = actor.Kind;
        ClosedById = actor.Id;
        ClosedByName = WarehouseOrder.RequireText(actor.DisplayName, nameof(actor.DisplayName), 120);
        ClosedAtUtc = now;

        foreach (var allocation in allocations)
        {
            _items.Add(PalletItem.Create(Guid.NewGuid(), Id, WarehouseOrderId, allocation.WarehouseOrderItemId,
                allocation.Quantity, allocation.UnitWeightKg));
        }

        GoodsWeightKg = NormalizeWeight(_items.Sum(x => x.LineWeightKg));
        TotalWeightKg = NormalizeWeight(GoodsWeightKg + EmptyPalletWeightKg);
    }

    public Guid Id { get; private set; }
    public Guid OperationId { get; private set; }
    public Guid WarehouseOrderId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public PalletStatus Status { get; private set; }
    public decimal EmptyPalletWeightKg { get; private set; }
    public decimal GoodsWeightKg { get; private set; }
    public decimal TotalWeightKg { get; private set; }
    public ActorKind ClosedByKind { get; private set; }
    public Guid ClosedById { get; private set; }
    public string ClosedByName { get; private set; } = string.Empty;
    public DateTimeOffset ClosedAtUtc { get; private set; }
    public IReadOnlyCollection<PalletItem> Items => _items;

    public static Pallet CreateClosed(Guid id, Guid operationId, Guid warehouseOrderId, string number,
        decimal emptyPalletWeightKg, IReadOnlyCollection<PalletItemAllocation> allocations,
        PickingActor actor, DateTimeOffset now) =>
        new(id, operationId, warehouseOrderId, number, emptyPalletWeightKg, allocations, actor, now);

    public static decimal NormalizeWeight(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static void ValidateActor(PickingActor actor)
    {
        if (actor.Id == Guid.Empty || actor.Kind is ActorKind.System)
            throw new ArgumentException("A pallet actor is required.", nameof(actor));
    }
}

public sealed class PalletItem
{
    private PalletItem() { }

    private PalletItem(Guid id, Guid palletId, Guid warehouseOrderId, Guid warehouseOrderItemId,
        decimal quantity, decimal unitWeightKg)
    {
        if (id == Guid.Empty || palletId == Guid.Empty || warehouseOrderId == Guid.Empty || warehouseOrderItemId == Guid.Empty)
            throw new ArgumentException("Pallet item identifiers are required.");
        if (decimal.Round(quantity, 4) != quantity || quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity),
                "Pallet item quantity must contain at most four decimal places and be greater than zero.");
        if (unitWeightKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitWeightKg),
                "Unit weight must be greater than zero to close a pallet.");

        Id = id;
        PalletId = palletId;
        WarehouseOrderId = warehouseOrderId;
        WarehouseOrderItemId = warehouseOrderItemId;
        Quantity = quantity;
        UnitWeightKg = Pallet.NormalizeWeight(unitWeightKg);
        LineWeightKg = Pallet.NormalizeWeight(quantity * UnitWeightKg);
    }

    public Guid Id { get; private set; }
    public Guid PalletId { get; private set; }
    public Guid WarehouseOrderId { get; private set; }
    public Guid WarehouseOrderItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitWeightKg { get; private set; }
    public decimal LineWeightKg { get; private set; }

    internal static PalletItem Create(Guid id, Guid palletId, Guid warehouseOrderId, Guid warehouseOrderItemId,
        decimal quantity, decimal unitWeightKg) =>
        new(id, palletId, warehouseOrderId, warehouseOrderItemId, quantity, unitWeightKg);
}
