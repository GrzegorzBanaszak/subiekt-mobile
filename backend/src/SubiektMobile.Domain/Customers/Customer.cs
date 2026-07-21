namespace SubiektMobile.Domain.Customers;

public enum VdaLabelProfile
{
    Vda4902
}

public sealed record CustomerSiteProfileInput(
    string? RecipientName,
    string? Street,
    string? PostalCode,
    string? City,
    string? DefaultDock,
    string? ReceivingHours,
    string? SupplierNumber,
    string? DefaultPalletType,
    decimal? MaximumPalletHeightCm,
    bool RequiresStretchFilm,
    bool RequiresStraps,
    bool RequiresCornerProtectors,
    string? LoadSecuringNotes,
    VdaLabelProfile? LabelProfile);

public sealed class Customer
{
    private readonly List<CustomerSite> _sites = [];

    private Customer() { }

    private Customer(Guid id, string code, string name, string? taxId, int? subiektContractorId,
        string? internalNotes, bool isActive, DateTimeOffset now)
    {
        if (id == Guid.Empty) throw new ArgumentException("Customer identifier is required.", nameof(id));
        Id = id;
        SetDetails(code, name, taxId, subiektContractorId, internalNotes);
        IsActive = isActive;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public int? SubiektContractorId { get; private set; }
    public string? InternalNotes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<CustomerSite> Sites => _sites;

    public static Customer Create(Guid id, string code, string name, string? taxId, int? subiektContractorId,
        string? internalNotes, bool isActive, DateTimeOffset now) =>
        new(id, code, name, taxId, subiektContractorId, internalNotes, isActive, now);

    public void Update(string code, string name, string? taxId, int? subiektContractorId, string? internalNotes,
        DateTimeOffset now)
    {
        SetDetails(code, name, taxId, subiektContractorId, internalNotes);
        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }

    public CustomerSite AddSite(Guid id, string code, string name, string countryCode, bool isActive, DateTimeOffset now)
    {
        var site = CustomerSite.Create(id, Id, code, name, countryCode, isActive, now);
        _sites.Add(site);
        Touch(now);
        return site;
    }

    private void SetDetails(string code, string name, string? taxId, int? subiektContractorId, string? internalNotes)
    {
        Code = CustomerRules.RequireCode(code, nameof(code));
        NormalizedCode = CustomerRules.Normalize(Code);
        Name = CustomerRules.RequireText(name, nameof(name), 120);
        TaxId = CustomerRules.OptionalText(taxId, nameof(taxId), 32);
        if (subiektContractorId is <= 0) throw new ArgumentOutOfRangeException(nameof(subiektContractorId));
        SubiektContractorId = subiektContractorId;
        InternalNotes = CustomerRules.OptionalText(internalNotes, nameof(internalNotes), 2000);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now;
        Version++;
    }
}

public sealed class CustomerSite
{
    private CustomerSite() { }

    private CustomerSite(Guid id, Guid customerId, string code, string name, string countryCode, bool isActive,
        DateTimeOffset now)
    {
        if (id == Guid.Empty || customerId == Guid.Empty) throw new ArgumentException("Site identifiers are required.");
        Id = id;
        CustomerId = customerId;
        SetDetails(code, name, countryCode);
        IsActive = isActive;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public CustomerLogisticsProfile? LogisticsProfile { get; private set; }

    internal static CustomerSite Create(Guid id, Guid customerId, string code, string name, string countryCode,
        bool isActive, DateTimeOffset now) => new(id, customerId, code, name, countryCode, isActive, now);

    public void Update(string code, string name, string countryCode, DateTimeOffset now)
    {
        SetDetails(code, name, countryCode);
        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }

    public void ConfigureLogisticsProfile(CustomerSiteProfileInput input, DateTimeOffset now)
    {
        LogisticsProfile ??= CustomerLogisticsProfile.Create(Guid.NewGuid(), Id);
        LogisticsProfile.Configure(input);
        Touch(now);
    }

    private void SetDetails(string code, string name, string countryCode)
    {
        Code = CustomerRules.RequireCode(code, nameof(code));
        NormalizedCode = CustomerRules.Normalize(Code);
        Name = CustomerRules.RequireText(name, nameof(name), 120);
        CountryCode = CustomerRules.RequireCountryCode(countryCode);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now;
        Version++;
    }
}

public sealed class CustomerLogisticsProfile
{
    private CustomerLogisticsProfile() { }

    private CustomerLogisticsProfile(Guid id, Guid customerSiteId)
    {
        Id = id;
        CustomerSiteId = customerSiteId;
    }

    public Guid Id { get; private set; }
    public Guid CustomerSiteId { get; private set; }
    public string? RecipientName { get; private set; }
    public string? Street { get; private set; }
    public string? PostalCode { get; private set; }
    public string? City { get; private set; }
    public string? DefaultDock { get; private set; }
    public string? ReceivingHours { get; private set; }
    public string? SupplierNumber { get; private set; }
    public string? DefaultPalletType { get; private set; }
    public decimal? MaximumPalletHeightCm { get; private set; }
    public bool RequiresStretchFilm { get; private set; }
    public bool RequiresStraps { get; private set; }
    public bool RequiresCornerProtectors { get; private set; }
    public string? LoadSecuringNotes { get; private set; }
    public VdaLabelProfile? LabelProfile { get; private set; }
    public bool IsComplete => RecipientName is not null && Street is not null && PostalCode is not null && City is not null
        && DefaultDock is not null && SupplierNumber is not null && LabelProfile is VdaLabelProfile.Vda4902;

    internal static CustomerLogisticsProfile Create(Guid id, Guid customerSiteId) => new(id, customerSiteId);

    public void Configure(CustomerSiteProfileInput input)
    {
        RecipientName = CustomerRules.OptionalText(input.RecipientName, nameof(input.RecipientName), 120);
        Street = CustomerRules.OptionalText(input.Street, nameof(input.Street), 160);
        PostalCode = CustomerRules.OptionalText(input.PostalCode, nameof(input.PostalCode), 20);
        City = CustomerRules.OptionalText(input.City, nameof(input.City), 80);
        DefaultDock = CustomerRules.OptionalText(input.DefaultDock, nameof(input.DefaultDock), 120);
        ReceivingHours = CustomerRules.OptionalText(input.ReceivingHours, nameof(input.ReceivingHours), 160);
        SupplierNumber = CustomerRules.OptionalText(input.SupplierNumber, nameof(input.SupplierNumber), 64);
        DefaultPalletType = CustomerRules.OptionalText(input.DefaultPalletType, nameof(input.DefaultPalletType), 120);
        if (input.MaximumPalletHeightCm is <= 0 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(input.MaximumPalletHeightCm));
        MaximumPalletHeightCm = input.MaximumPalletHeightCm;
        RequiresStretchFilm = input.RequiresStretchFilm;
        RequiresStraps = input.RequiresStraps;
        RequiresCornerProtectors = input.RequiresCornerProtectors;
        LoadSecuringNotes = CustomerRules.OptionalText(input.LoadSecuringNotes, nameof(input.LoadSecuringNotes), 1000);
        LabelProfile = input.LabelProfile;
    }
}

internal static class CustomerRules
{
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public static string RequireCode(string value, string parameterName)
    {
        var result = RequireText(value, parameterName, 32);
        if (result.Length < 2 || result.Any(character => !char.IsLetterOrDigit(character) && character is not '_' and not '-'))
            throw new ArgumentException("Code must contain 2 to 32 letters, digits, '_' or '-'.", parameterName);
        return result;
    }

    public static string RequireCountryCode(string value)
    {
        var result = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (result.Length != 2 || result.Any(character => !char.IsAsciiLetter(character)))
            throw new ArgumentException("Country code must be an ISO 3166-1 alpha-2 code.", nameof(value));
        return result;
    }

    public static string RequireText(string value, string parameterName, int maximumLength)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length == 0 || result.Length > maximumLength)
            throw new ArgumentException($"Value must contain between 1 and {maximumLength} characters.", parameterName);
        return result;
    }

    public static string? OptionalText(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return RequireText(value, parameterName, maximumLength);
    }
}
