namespace SubiektMobile.Domain.Customers;

public sealed class PackagingType
{
    private PackagingType() { }
    private PackagingType(Guid id, string code, string name, decimal tareWeightKg, decimal? defaultCapacity, bool isActive, DateTimeOffset now)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Packaging type identifier is required.", nameof(id)) : id;
        SetDetails(code, name, tareWeightKg, defaultCapacity);
        IsActive = isActive; CreatedAtUtc = UpdatedAtUtc = now; Version = 1;
    }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal TareWeightKg { get; private set; }
    public decimal? DefaultCapacity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static PackagingType Create(Guid id, string code, string name, decimal tareWeightKg, decimal? defaultCapacity, bool isActive, DateTimeOffset now) => new(id, code, name, tareWeightKg, defaultCapacity, isActive, now);
    public void Update(string code, string name, decimal tareWeightKg, decimal? defaultCapacity, DateTimeOffset now) { SetDetails(code, name, tareWeightKg, defaultCapacity); Touch(now); }
    public void SetActive(bool isActive, DateTimeOffset now) { IsActive = isActive; Touch(now); }
    private void SetDetails(string code, string name, decimal tareWeightKg, decimal? defaultCapacity)
    {
        Code = PackagingRules.RequiredCode(code, nameof(code)); NormalizedCode = PackagingRules.Normalize(Code);
        Name = CustomerRules.RequireText(name, nameof(name), 120);
        if (tareWeightKg < 0 || tareWeightKg > 100000) throw new ArgumentOutOfRangeException(nameof(tareWeightKg));
        if (defaultCapacity is <= 0 or > 100000000) throw new ArgumentOutOfRangeException(nameof(defaultCapacity));
        TareWeightKg = tareWeightKg; DefaultCapacity = defaultCapacity;
    }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class CustomerPackagingCode
{
    private CustomerPackagingCode() { }
    private CustomerPackagingCode(Guid id, Guid customerId, Guid? customerSiteId, Guid packagingTypeId, string code, bool isActive, DateTimeOffset now)
    {
        if (id == Guid.Empty || customerId == Guid.Empty || packagingTypeId == Guid.Empty) throw new ArgumentException("Packaging code identifiers are required.");
        Id = id; CustomerId = customerId; CustomerSiteId = customerSiteId; PackagingTypeId = packagingTypeId; SetCode(code); IsActive = isActive; CreatedAtUtc = UpdatedAtUtc = now; Version = 1;
    }
    public Guid Id { get; private set; } public Guid CustomerId { get; private set; } public Guid? CustomerSiteId { get; private set; } public Guid PackagingTypeId { get; private set; }
    public string Code { get; private set; } = string.Empty; public string NormalizedCode { get; private set; } = string.Empty; public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; }
    public static CustomerPackagingCode Create(Guid id, Guid customerId, Guid? siteId, Guid packagingTypeId, string code, bool active, DateTimeOffset now) => new(id, customerId, siteId, packagingTypeId, code, active, now);
    public void Update(Guid packagingTypeId, string code, DateTimeOffset now) { if (packagingTypeId == Guid.Empty) throw new ArgumentException("Packaging type is required.", nameof(packagingTypeId)); PackagingTypeId = packagingTypeId; SetCode(code); Touch(now); }
    public void SetActive(bool active, DateTimeOffset now) { IsActive = active; Touch(now); }
    private void SetCode(string code) { Code = PackagingRules.RequiredCode(code, nameof(code)); NormalizedCode = PackagingRules.Normalize(Code); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class CustomerPartMapping
{
    private CustomerPartMapping() { }
    private CustomerPartMapping(Guid id, Guid customerId, Guid? siteId, string partNumber, int productId, Guid? defaultPackagingTypeId, string? engineeringChange, bool active, DateTimeOffset now)
    {
        if (id == Guid.Empty || customerId == Guid.Empty || productId <= 0) throw new ArgumentException("Part mapping identifiers are required.");
        Id = id; CustomerId = customerId; CustomerSiteId = siteId; SetDetails(partNumber, productId, defaultPackagingTypeId, engineeringChange); IsActive = active; CreatedAtUtc = UpdatedAtUtc = now; Version = 1;
    }
    public Guid Id { get; private set; } public Guid CustomerId { get; private set; } public Guid? CustomerSiteId { get; private set; }
    public string CustomerPartNumber { get; private set; } = string.Empty; public string NormalizedCustomerPartNumber { get; private set; } = string.Empty; public int ProductId { get; private set; }
    public Guid? DefaultPackagingTypeId { get; private set; } public string? EngineeringChange { get; private set; } public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; }
    public static CustomerPartMapping Create(Guid id, Guid customerId, Guid? siteId, string partNumber, int productId, Guid? packagingTypeId, string? engineeringChange, bool active, DateTimeOffset now) => new(id, customerId, siteId, partNumber, productId, packagingTypeId, engineeringChange, active, now);
    public void Update(string partNumber, int productId, Guid? packagingTypeId, string? engineeringChange, DateTimeOffset now) { SetDetails(partNumber, productId, packagingTypeId, engineeringChange); Touch(now); }
    public void SetActive(bool active, DateTimeOffset now) { IsActive = active; Touch(now); }
    private void SetDetails(string partNumber, int productId, Guid? packagingTypeId, string? engineeringChange)
    {
        CustomerPartNumber = CustomerRules.RequireText(partNumber, nameof(partNumber), 80); NormalizedCustomerPartNumber = PackagingRules.Normalize(CustomerPartNumber);
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId)); ProductId = productId; DefaultPackagingTypeId = packagingTypeId;
        EngineeringChange = CustomerRules.OptionalText(engineeringChange, nameof(engineeringChange), 80);
    }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

internal static class PackagingRules
{
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
    public static string RequiredCode(string value, string parameterName)
    {
        var result = CustomerRules.RequireText(value, parameterName, 64);
        if (result.Any(char.IsControl)) throw new ArgumentException("Code must not contain control characters.", parameterName);
        return result;
    }
}
