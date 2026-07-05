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

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    private Order(Guid id, string number, string customerName, DateOnly dueDate,
        Guid createdById, string createdByName, DateTimeOffset createdAtUtc)
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

    public static Order Create(Guid id, string number, string customerName, DateOnly dueDate,
        Guid actorId, string actorName, DateTimeOffset now)
    {
        if (id == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Identifiers are required.");
        return new Order(id, number, customerName, dueDate, actorId, actorName, now);
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
        Status = OrderStatus.ReadyForPicking;
        PublishedAtUtc = now;
        Touch(actorId, actorName, now);
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
