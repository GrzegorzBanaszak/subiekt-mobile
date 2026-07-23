namespace SubiektMobile.Domain.CustomerOrders;

public enum CustomerOrderStatus
{
    Draft,
    ReadyForConversion,
    Converted,
    Cancelled
}

public sealed class CustomerOrder
{
    private readonly List<CustomerOrderItem> _items = [];

    private CustomerOrder() { }

    private CustomerOrder(Guid id, Guid customerId, Guid customerSiteId, string? customerOrderNumber,
        string? deliveryNoteNumber, DateOnly requestedDeliveryDate, string? customerNotes,
        Guid actorId, string actorName, DateTimeOffset now)
    {
        if (id == Guid.Empty || customerId == Guid.Empty || customerSiteId == Guid.Empty || actorId == Guid.Empty)
            throw new ArgumentException("Customer order identifiers are required.");

        Id = id;
        CustomerId = customerId;
        CustomerSiteId = customerSiteId;
        SetHeader(customerOrderNumber, deliveryNoteNumber, requestedDeliveryDate, customerNotes);
        Status = CustomerOrderStatus.Draft;
        CreatedById = actorId;
        CreatedByName = CustomerOrderRules.RequireText(actorName, nameof(actorName), 120);
        CreatedAtUtc = now;
        UpdatedById = actorId;
        UpdatedByName = CreatedByName;
        UpdatedAtUtc = now;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid CustomerSiteId { get; private set; }
    public string? CustomerOrderNumber { get; private set; }
    public string? DeliveryNoteNumber { get; private set; }
    public DateOnly RequestedDeliveryDate { get; private set; }
    public string? CustomerNotes { get; private set; }
    public CustomerOrderStatus Status { get; private set; }
    public Guid CreatedById { get; private set; }
    public string CreatedByName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid UpdatedById { get; private set; }
    public string UpdatedByName { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<CustomerOrderItem> Items => _items;

    public static CustomerOrder Create(Guid id, Guid customerId, Guid customerSiteId, string? customerOrderNumber,
        string? deliveryNoteNumber, DateOnly requestedDeliveryDate, string? customerNotes,
        Guid actorId, string actorName, DateTimeOffset now) =>
        new(id, customerId, customerSiteId, customerOrderNumber, deliveryNoteNumber, requestedDeliveryDate,
            customerNotes, actorId, actorName, now);

    public void Update(string? customerOrderNumber, string? deliveryNoteNumber, DateOnly requestedDeliveryDate,
        string? customerNotes, Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        SetHeader(customerOrderNumber, deliveryNoteNumber, requestedDeliveryDate, customerNotes);
        Touch(actorId, actorName, now);
    }

    public CustomerOrderItem AddItem(Guid id, string customerPartNumber, decimal quantity,
        Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        var item = CustomerOrderItem.Create(id, Id, customerPartNumber, quantity);
        _items.Add(item);
        Touch(actorId, actorName, now);
        return item;
    }

    public void UpdateItem(Guid itemId, string customerPartNumber, decimal quantity,
        Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        FindItem(itemId).Update(customerPartNumber, quantity);
        Touch(actorId, actorName, now);
    }

    public void RemoveItem(Guid itemId, Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        _items.Remove(FindItem(itemId));
        Touch(actorId, actorName, now);
    }

    public void MarkReady(Guid actorId, string actorName, DateTimeOffset now)
    {
        EnsureDraft();
        if (_items.Count == 0) throw new InvalidOperationException("Customer order must contain at least one item.");
        Status = CustomerOrderStatus.ReadyForConversion;
        Touch(actorId, actorName, now);
    }

    public void MarkConverted(Guid actorId, string actorName, DateTimeOffset now)
    {
        if (Status != CustomerOrderStatus.ReadyForConversion)
            throw new InvalidOperationException("Only a ready customer order can be converted.");
        Status = CustomerOrderStatus.Converted;
        Touch(actorId, actorName, now);
    }

    public void Cancel(Guid actorId, string actorName, DateTimeOffset now)
    {
        if (Status is CustomerOrderStatus.Converted or CustomerOrderStatus.Cancelled)
            throw new InvalidOperationException("Only a non-converted customer order can be cancelled.");
        Status = CustomerOrderStatus.Cancelled;
        Touch(actorId, actorName, now);
    }

    private CustomerOrderItem FindItem(Guid itemId) => _items.SingleOrDefault(x => x.Id == itemId)
        ?? throw new KeyNotFoundException("Customer order item was not found.");

    private void SetHeader(string? customerOrderNumber, string? deliveryNoteNumber,
        DateOnly requestedDeliveryDate, string? customerNotes)
    {
        CustomerOrderNumber = CustomerOrderRules.OptionalText(customerOrderNumber, nameof(customerOrderNumber), 80);
        DeliveryNoteNumber = CustomerOrderRules.OptionalText(deliveryNoteNumber, nameof(deliveryNoteNumber), 80);
        if (requestedDeliveryDate == default)
            throw new ArgumentException("Requested delivery date is required.", nameof(requestedDeliveryDate));
        RequestedDeliveryDate = requestedDeliveryDate;
        CustomerNotes = CustomerOrderRules.OptionalText(customerNotes, nameof(customerNotes), 2000);
    }

    private void EnsureDraft()
    {
        if (Status != CustomerOrderStatus.Draft)
            throw new InvalidOperationException("Only a draft customer order can be modified.");
    }

    private void Touch(Guid actorId, string actorName, DateTimeOffset now)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(actorId));
        UpdatedById = actorId;
        UpdatedByName = CustomerOrderRules.RequireText(actorName, nameof(actorName), 120);
        UpdatedAtUtc = now;
        Version++;
    }
}

public sealed class CustomerOrderItem
{
    private CustomerOrderItem() { }

    private CustomerOrderItem(Guid id, Guid customerOrderId, string customerPartNumber, decimal quantity)
    {
        if (id == Guid.Empty || customerOrderId == Guid.Empty)
            throw new ArgumentException("Customer order item identifiers are required.");
        Id = id;
        CustomerOrderId = customerOrderId;
        SetDetails(customerPartNumber, quantity);
    }

    public Guid Id { get; private set; }
    public Guid CustomerOrderId { get; private set; }
    public string CustomerPartNumber { get; private set; } = string.Empty;
    public string NormalizedCustomerPartNumber { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }

    internal static CustomerOrderItem Create(Guid id, Guid customerOrderId, string customerPartNumber, decimal quantity) =>
        new(id, customerOrderId, customerPartNumber, quantity);

    internal void Update(string customerPartNumber, decimal quantity) => SetDetails(customerPartNumber, quantity);

    private void SetDetails(string customerPartNumber, decimal quantity)
    {
        CustomerPartNumber = CustomerOrderRules.RequireText(customerPartNumber, nameof(customerPartNumber), 80);
        NormalizedCustomerPartNumber = CustomerPartNumber.ToUpperInvariant();
        if (quantity <= 0 || decimal.Round(quantity, 4) != quantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive and contain at most four decimal places.");
        Quantity = quantity;
    }
}

internal static class CustomerOrderRules
{
    public static string RequireText(string? value, string parameterName, int maximumLength)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length == 0 || result.Length > maximumLength)
            throw new ArgumentException($"Value must contain between 1 and {maximumLength} characters.", parameterName);
        return result;
    }

    public static string? OptionalText(string? value, string parameterName, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : RequireText(value, parameterName, maximumLength);
}
