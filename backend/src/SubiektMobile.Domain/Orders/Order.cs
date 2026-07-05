namespace SubiektMobile.Domain.Orders;

public enum OrderStatus
{
    Draft,
    ReadyForPicking
}

public enum OrderItemStatus
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

public sealed record OrderAssigneeCandidate(Guid EmployeeId, Guid OrganizationId, string EmployeeDisplayName);

public sealed class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderAssignee> _assignees = [];

    private Order() { }

    private Order(Guid id, string number, string customerName, DateOnly dueDate,
        Guid createdById, string createdByName, DateTimeOffset createdAtUtc,
        PickingMode pickingMode, IReadOnlyCollection<OrderAssigneeCandidate> assignees)
    {
        Id = id;
        Number = RequireText(number, nameof(number), 40);
        SetHeader(customerName, dueDate);
        Status = OrderStatus.Draft;
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
    public OrderStatus Status { get; private set; }
    public Guid CreatedById { get; private set; }
    public string CreatedByName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid UpdatedById { get; private set; }
    public string UpdatedByName { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;
    public PickingMode PickingMode { get; private set; }
    public IReadOnlyCollection<OrderAssignee> Assignees => _assignees;

    public static Order Create(Guid id, string number, string customerName, DateOnly dueDate,
        Guid actorId, string actorName, DateTimeOffset now,
        PickingMode pickingMode = PickingMode.SingleAssignee,
        IReadOnlyCollection<OrderAssigneeCandidate>? assignees = null)
    {
        if (id == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Identifiers are required.");
        return new Order(id, number, customerName, dueDate, actorId, actorName, now,
            pickingMode, assignees ?? []);
    }

    public void UpdateHeader(string customerName, DateOnly dueDate, Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        SetHeader(customerName, dueDate);
        Touch(actorId, actorName, now);
    }

    public OrderItem AddItem(int productId, string productName, string? productSymbol,
        decimal quantity, string unit, decimal? unitWeightKg, Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        if (_items.Any(x => x.ProductId == productId)) throw new InvalidOperationException("Product is already present in the order.");
        var item = OrderItem.Create(Guid.NewGuid(), Id, productId, productName, productSymbol, quantity, unit, unitWeightKg);
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
        Status = OrderStatus.ReadyForPicking;
        PublishedAtUtc = now;
        Touch(actorId, actorName, now);
    }

    public void ConfigurePicking(PickingMode mode, IReadOnlyCollection<OrderAssigneeCandidate> assignees,
        Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        SetPickingConfiguration(mode, assignees, actorId, actorName, now);
        Touch(actorId, actorName, now);
    }

    public void EnsureCanDelete()
    {
        if (Status != OrderStatus.Draft)
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
        if (Status != OrderStatus.Draft) throw new InvalidOperationException("Only a draft order can be modified.");
    }

    private void SetPickingConfiguration(PickingMode mode, IReadOnlyCollection<OrderAssigneeCandidate> assignees,
        Guid actorId, string actorName, DateTimeOffset now)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var distinct = assignees.DistinctBy(x => x.EmployeeId).ToList();
        if (distinct.Count != assignees.Count)
            throw new ArgumentException("An employee can be assigned only once.", nameof(assignees));
        if (mode == PickingMode.SingleAssignee && distinct.Count > 1)
            throw new ArgumentException("Single-assignee mode accepts at most one employee.", nameof(assignees));

        _assignees.Clear();
        _assignees.AddRange(distinct.Select(x => OrderAssignee.Create(
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

public sealed class OrderAssignee
{
    private OrderAssignee() { }

    private OrderAssignee(Guid id, Guid orderId, Guid employeeId, Guid organizationId,
        string employeeDisplayName, Guid assignedById, string assignedByName, DateTimeOffset assignedAtUtc)
    {
        if (employeeId == Guid.Empty || organizationId == Guid.Empty || assignedById == Guid.Empty)
            throw new ArgumentException("Assignment identifiers are required.");
        Id = id;
        OrderId = orderId;
        EmployeeId = employeeId;
        OrganizationId = organizationId;
        EmployeeDisplayName = Order.RequireText(employeeDisplayName, nameof(employeeDisplayName), 120);
        AssignedById = assignedById;
        AssignedByName = Order.RequireText(assignedByName, nameof(assignedByName), 120);
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string EmployeeDisplayName { get; private set; } = string.Empty;
    public Guid AssignedById { get; private set; }
    public string AssignedByName { get; private set; } = string.Empty;
    public DateTimeOffset AssignedAtUtc { get; private set; }

    internal static OrderAssignee Create(Guid id, Guid orderId, Guid employeeId, Guid organizationId,
        string employeeDisplayName, Guid assignedById, string assignedByName, DateTimeOffset assignedAtUtc) =>
        new(id, orderId, employeeId, organizationId, employeeDisplayName, assignedById, assignedByName, assignedAtUtc);
}

public sealed class OrderItem
{
    private OrderItem() { }

    private OrderItem(Guid id, Guid orderId, int productId, string productName, string? productSymbol,
        decimal quantity, string unit, decimal? unitWeightKg)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (unitWeightKg < 0) throw new ArgumentOutOfRangeException(nameof(unitWeightKg), "Unit weight cannot be negative.");
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        ProductName = Order.RequireText(productName, nameof(productName), 300);
        ProductSymbol = string.IsNullOrWhiteSpace(productSymbol) ? null : productSymbol.Trim();
        Quantity = quantity;
        Unit = Order.RequireText(unit, nameof(unit), 20);
        UnitWeightKg = unitWeightKg;
        Status = OrderItemStatus.ToPick;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductSymbol { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal? UnitWeightKg { get; private set; }
    public OrderItemStatus Status { get; private set; }

    internal static OrderItem Create(Guid id, Guid orderId, int productId, string productName,
        string? productSymbol, decimal quantity, string unit, decimal? unitWeightKg) =>
        new(id, orderId, productId, productName, productSymbol, quantity, unit, unitWeightKg);
}
