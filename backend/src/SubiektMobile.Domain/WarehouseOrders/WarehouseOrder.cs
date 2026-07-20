using SubiektMobile.Domain.Identity;

namespace SubiektMobile.Domain.WarehouseOrders;

public enum WarehouseOrderStatus
{
    Draft,
    ReadyForPicking
}

public enum WarehouseOrderItemStatus
{
    ToPick,
    Picking,
    Packed,
    AssignedToPallet
}

public enum PickingMode
{
    SingleAssignee,
    SharedTeam
}

public sealed record WarehouseOrderAssigneeCandidate(Guid EmployeeId, Guid OrganizationId, string EmployeeDisplayName);

public sealed class WarehouseOrder
{
    private readonly List<WarehouseOrderItem> _items = [];
    private readonly List<WarehouseOrderAssignee> _assignees = [];

    private WarehouseOrder() { }

    private WarehouseOrder(Guid id, string number, string customerName, DateOnly dueDate,
        Guid createdById, string createdByName, DateTimeOffset createdAtUtc,
        PickingMode pickingMode, IReadOnlyCollection<WarehouseOrderAssigneeCandidate> assignees)
    {
        Id = id;
        Number = RequireText(number, nameof(number), 40);
        SetHeader(customerName, dueDate);
        Status = WarehouseOrderStatus.Draft;
        CreatedById = createdById;
        CreatedByName = RequireText(createdByName, nameof(createdByName), 120);
        CreatedAtUtc = createdAtUtc;
        UpdatedById = createdById;
        UpdatedByName = CreatedByName;
        UpdatedAtUtc = createdAtUtc;
        Version = 1;
        SetPickingConfiguration(pickingMode, assignees, createdById, CreatedByName, createdAtUtc);
    }

    public Guid Id { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public WarehouseOrderStatus Status { get; private set; }
    public Guid CreatedById { get; private set; }
    public string CreatedByName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid UpdatedById { get; private set; }
    public string UpdatedByName { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<WarehouseOrderItem> Items => _items;
    public PickingMode PickingMode { get; private set; }
    public IReadOnlyCollection<WarehouseOrderAssignee> Assignees => _assignees;

    public WarehouseOrderItem ReserveItem(Guid itemId, PickingActor actor, DateTimeOffset now)
    {
        EnsurePublished();
        if (PickingMode != PickingMode.SharedTeam)
            throw new InvalidOperationException("Single-assignee orders do not require item reservation.");
        var item = FindItem(itemId);
        item.Reserve(actor, now);
        return item;
    }

    public WarehouseOrderItem ReleaseItem(Guid itemId, PickingActor actor, bool canOverride, DateTimeOffset now)
    {
        EnsurePublished();
        var item = FindItem(itemId);
        item.Release(actor, canOverride, now);
        return item;
    }

    public WarehouseOrderItem PackItem(Guid itemId, decimal packedQuantity, PickingActor actor,
        bool canOverride, DateTimeOffset now)
    {
        EnsurePublished();
        var item = FindItem(itemId);
        item.Pack(packedQuantity, actor, PickingMode == PickingMode.SharedTeam, canOverride, now);
        return item;
    }

    public WarehouseOrderItem UndoPackedItem(Guid itemId, PickingActor actor, bool canOverride, DateTimeOffset now,
        decimal palletizedQuantity = 0)
    {
        EnsurePublished();
        var item = FindItem(itemId);
        item.UndoPacked(actor, canOverride, now, palletizedQuantity);
        return item;
    }

    public WarehouseOrderItem AssignPackedQuantityToPallet(Guid itemId, decimal totalPalletizedQuantity)
    {
        EnsurePublished();
        var item = FindItem(itemId);
        item.AssignPackedQuantityToPallet(totalPalletizedQuantity);
        return item;
    }

    public static WarehouseOrder Create(Guid id, string number, string customerName, DateOnly dueDate,
        Guid actorId, string actorName, DateTimeOffset now,
        PickingMode pickingMode = PickingMode.SingleAssignee,
        IReadOnlyCollection<WarehouseOrderAssigneeCandidate>? assignees = null)
    {
        if (id == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Identifiers are required.");
        return new WarehouseOrder(id, number, customerName, dueDate, actorId, actorName, now,
            pickingMode, assignees ?? []);
    }

    public void UpdateHeader(string customerName, DateOnly dueDate, Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        SetHeader(customerName, dueDate);
        Touch(actorId, actorName, now);
    }

    public WarehouseOrderItem AddItem(int productId, string productName, string? productSymbol,
        decimal quantity, string unit, decimal? unitWeightKg, Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        if (_items.Any(x => x.ProductId == productId)) throw new InvalidOperationException("Product is already present in the order.");
        var item = WarehouseOrderItem.Create(Guid.NewGuid(), Id, productId, productName, productSymbol, quantity, unit, unitWeightKg);
        _items.Add(item);
        Touch(actorId, actorName, now);
        return item;
    }

    public void RemoveItem(Guid itemId, Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        var item = _items.SingleOrDefault(x => x.Id == itemId)
            ?? throw new KeyNotFoundException("Order item was not found.");
        _items.Remove(item);
        Touch(actorId, actorName, now);
    }

    public void Publish(DateOnly today, Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        if (_items.Count == 0) throw new InvalidOperationException("Order must contain at least one item.");
        if (DueDate < today) throw new InvalidOperationException("Due date cannot be in the past.");
        ValidateAssigneesForPublishing();
        Status = WarehouseOrderStatus.ReadyForPicking;
        PublishedAtUtc = now;
        Touch(actorId, actorName, now);
    }

    public void ConfigurePicking(PickingMode mode, IReadOnlyCollection<WarehouseOrderAssigneeCandidate> assignees,
        Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        SetPickingConfiguration(mode, assignees, actorId, actorName, now);
        Touch(actorId, actorName, now);
    }

    public void EnsureCanDelete()
    {
        if (Status != WarehouseOrderStatus.Draft)
            throw new InvalidOperationException("Only a draft order can be deleted.");
    }

    private void SetHeader(string customerName, DateOnly dueDate)
    {
        CustomerName = RequireText(customerName, nameof(customerName), 200);
        if (dueDate == default) throw new ArgumentException("Due date is required.", nameof(dueDate));
        DueDate = dueDate;
    }

    private void EnsureDraft()
    {
        if (Status != WarehouseOrderStatus.Draft) throw new InvalidOperationException("Only a draft order can be modified.");
    }

    private void EnsurePublished()
    {
        if (Status != WarehouseOrderStatus.ReadyForPicking)
            throw new InvalidOperationException("Only a published order can be picked.");
    }

    private WarehouseOrderItem FindItem(Guid itemId) => _items.SingleOrDefault(x => x.Id == itemId)
        ?? throw new KeyNotFoundException("Order item was not found.");

    private void SetPickingConfiguration(PickingMode mode, IReadOnlyCollection<WarehouseOrderAssigneeCandidate> assignees,
        Guid actorId, string actorName, DateTimeOffset now)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var distinct = assignees.DistinctBy(x => x.EmployeeId).ToList();
        if (distinct.Count != assignees.Count)
            throw new ArgumentException("An employee can be assigned only once.", nameof(assignees));
        if (mode == PickingMode.SingleAssignee && distinct.Count > 1)
            throw new ArgumentException("Single-assignee mode accepts at most one employee.", nameof(assignees));

        _assignees.Clear();
        _assignees.AddRange(distinct.Select(x => WarehouseOrderAssignee.Create(
            Guid.NewGuid(), Id, x.EmployeeId, x.OrganizationId, x.EmployeeDisplayName,
            actorId, actorName, now)));
        PickingMode = mode;
    }

    private void ValidateAssigneesForPublishing()
    {
        if (PickingMode == PickingMode.SingleAssignee && _assignees.Count != 1)
            throw new InvalidOperationException("Single-assignee order requires exactly one assigned employee.");
        if (PickingMode == PickingMode.SharedTeam && _assignees.Count == 0)
            throw new InvalidOperationException("Shared order requires at least one assigned employee.");
    }

    private void Touch(Guid actorId, string actorName, DateTimeOffset now)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(actorId));
        UpdatedById = actorId;
        UpdatedByName = RequireText(actorName, nameof(actorName), 120);
        UpdatedAtUtc = now;
        Version++;
    }

    internal static string RequireText(string? value, string parameterName, int maximumLength)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length == 0 || result.Length > maximumLength)
            throw new ArgumentException($"Value must contain between 1 and {maximumLength} characters.", parameterName);
        return result;
    }
}

public sealed class WarehouseOrderAssignee
{
    private WarehouseOrderAssignee() { }

    private WarehouseOrderAssignee(Guid id, Guid warehouseOrderId, Guid employeeId, Guid organizationId,
        string employeeDisplayName, Guid assignedById, string assignedByName, DateTimeOffset assignedAtUtc)
    {
        if (employeeId == Guid.Empty || organizationId == Guid.Empty || assignedById == Guid.Empty)
            throw new ArgumentException("Assignment identifiers are required.");
        Id = id;
        WarehouseOrderId = warehouseOrderId;
        EmployeeId = employeeId;
        OrganizationId = organizationId;
        EmployeeDisplayName = WarehouseOrder.RequireText(employeeDisplayName, nameof(employeeDisplayName), 120);
        AssignedById = assignedById;
        AssignedByName = WarehouseOrder.RequireText(assignedByName, nameof(assignedByName), 120);
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseOrderId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string EmployeeDisplayName { get; private set; } = string.Empty;
    public Guid AssignedById { get; private set; }
    public string AssignedByName { get; private set; } = string.Empty;
    public DateTimeOffset AssignedAtUtc { get; private set; }

    internal static WarehouseOrderAssignee Create(Guid id, Guid warehouseOrderId, Guid employeeId, Guid organizationId,
        string employeeDisplayName, Guid assignedById, string assignedByName, DateTimeOffset assignedAtUtc) =>
        new(id, warehouseOrderId, employeeId, organizationId, employeeDisplayName, assignedById, assignedByName, assignedAtUtc);
}

public sealed class WarehouseOrderItem
{
    private WarehouseOrderItem() { }

    private WarehouseOrderItem(Guid id, Guid warehouseOrderId, int productId, string productName, string? productSymbol,
        decimal quantity, string unit, decimal? unitWeightKg)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (unitWeightKg < 0) throw new ArgumentOutOfRangeException(nameof(unitWeightKg), "Unit weight cannot be negative.");
        Id = id;
        WarehouseOrderId = warehouseOrderId;
        ProductId = productId;
        ProductName = WarehouseOrder.RequireText(productName, nameof(productName), 300);
        ProductSymbol = string.IsNullOrWhiteSpace(productSymbol) ? null : productSymbol.Trim();
        Quantity = quantity;
        Unit = WarehouseOrder.RequireText(unit, nameof(unit), 20);
        UnitWeightKg = unitWeightKg;
        Status = WarehouseOrderItemStatus.ToPick;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseOrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductSymbol { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal? UnitWeightKg { get; private set; }
    public WarehouseOrderItemStatus Status { get; private set; }
    public long Version { get; private set; }
    public ActorKind? ReservedByKind { get; private set; }
    public Guid? ReservedById { get; private set; }
    public string? ReservedByName { get; private set; }
    public DateTimeOffset? ReservedAtUtc { get; private set; }
    public decimal? PackedQuantity { get; private set; }
    public ActorKind? PackedByKind { get; private set; }
    public Guid? PackedById { get; private set; }
    public string? PackedByName { get; private set; }
    public DateTimeOffset? PackedAtUtc { get; private set; }

    internal static WarehouseOrderItem Create(Guid id, Guid warehouseOrderId, int productId, string productName,
        string? productSymbol, decimal quantity, string unit, decimal? unitWeightKg) =>
        new(id, warehouseOrderId, productId, productName, productSymbol, quantity, unit, unitWeightKg);

    internal void Reserve(PickingActor actor, DateTimeOffset now)
    {
        if (Status != WarehouseOrderItemStatus.ToPick)
            throw new InvalidOperationException("Only an available item can be reserved.");
        ValidateActor(actor);
        Status = WarehouseOrderItemStatus.Picking;
        ReservedByKind = actor.Kind;
        ReservedById = actor.Id;
        ReservedByName = WarehouseOrder.RequireText(actor.DisplayName, nameof(actor.DisplayName), 120);
        ReservedAtUtc = now;
        Version++;
    }

    internal void Release(PickingActor actor, bool canOverride, DateTimeOffset now)
    {
        if (Status != WarehouseOrderItemStatus.Picking)
            throw new InvalidOperationException("Only a reserved item can be released.");
        ValidateActor(actor);
        if (!canOverride && ReservedById != actor.Id)
            throw new InvalidOperationException("Only the current item owner can release it.");
        Status = WarehouseOrderItemStatus.ToPick;
        ClearReservation();
        Version++;
    }

    internal void Pack(decimal packedQuantity, PickingActor actor, bool reservationRequired,
        bool canOverride, DateTimeOffset now)
    {
        ValidateActor(actor);
        var alreadyPacked = PackedQuantity ?? 0;
        var remainingQuantity = Quantity - alreadyPacked;
        if (decimal.Round(packedQuantity, 4) != packedQuantity || packedQuantity <= 0 ||
            packedQuantity > remainingQuantity)
            throw new ArgumentOutOfRangeException(nameof(packedQuantity),
                "Packed quantity must contain at most four decimal places and be greater than zero without exceeding the remaining quantity.");
        if (reservationRequired)
        {
            if (Status != WarehouseOrderItemStatus.Picking)
                throw new InvalidOperationException("A shared item must be reserved before packing.");
            if (!canOverride && ReservedById != actor.Id)
                throw new InvalidOperationException("Only the current item owner can pack it.");
        }
        else if (Status != WarehouseOrderItemStatus.ToPick)
        {
            throw new InvalidOperationException("Only an available item can be packed.");
        }

        PackedQuantity = alreadyPacked + packedQuantity;
        PackedByKind = actor.Kind;
        PackedById = actor.Id;
        PackedByName = WarehouseOrder.RequireText(actor.DisplayName, nameof(actor.DisplayName), 120);
        PackedAtUtc = now;
        if (PackedQuantity == Quantity)
        {
            Status = WarehouseOrderItemStatus.Packed;
            ClearReservation();
        }
        Version++;
    }

    internal void UndoPacked(PickingActor actor, bool canOverride, DateTimeOffset now,
        decimal palletizedQuantity)
    {
        if (Status == WarehouseOrderItemStatus.AssignedToPallet || PackedQuantity is null or <= 0)
            throw new InvalidOperationException("Only an item with a packed quantity can be restored.");
        if (palletizedQuantity > 0)
            throw new InvalidOperationException("Packed quantity assigned to a pallet cannot be restored.");
        ValidateActor(actor);
        if (!canOverride && PackedById != actor.Id)
            throw new InvalidOperationException("Only the employee who packed the item can restore it.");
        if (Status == WarehouseOrderItemStatus.Packed) Status = WarehouseOrderItemStatus.ToPick;
        PackedQuantity = null;
        PackedByKind = null;
        PackedById = null;
        PackedByName = null;
        PackedAtUtc = null;
        Version++;
    }

    internal void AssignPackedQuantityToPallet(decimal totalPalletizedQuantity)
    {
        if (decimal.Round(totalPalletizedQuantity, 4) != totalPalletizedQuantity || totalPalletizedQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(totalPalletizedQuantity),
                "Palletized quantity must contain at most four decimal places and cannot be negative.");
        var packedQuantity = PackedQuantity ?? 0;
        if (packedQuantity <= 0)
            throw new InvalidOperationException("Only packed quantity can be assigned to a pallet.");
        if (totalPalletizedQuantity > packedQuantity)
            throw new InvalidOperationException("Palletized quantity cannot exceed packed quantity.");
        if (packedQuantity == Quantity && totalPalletizedQuantity == Quantity)
        {
            Status = WarehouseOrderItemStatus.AssignedToPallet;
            ClearReservation();
        }
        Version++;
    }

    private void ClearReservation()
    {
        ReservedByKind = null;
        ReservedById = null;
        ReservedByName = null;
        ReservedAtUtc = null;
    }

    private static void ValidateActor(PickingActor actor)
    {
        if (actor.Id == Guid.Empty || actor.Kind is ActorKind.System)
            throw new ArgumentException("A picking actor is required.", nameof(actor));
    }
}

public sealed record PickingActor(ActorKind Kind, Guid Id, string DisplayName);
