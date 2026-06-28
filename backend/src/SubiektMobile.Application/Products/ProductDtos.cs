namespace SubiektMobile.Application.Products;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ProductListItemDto(
    int Id,
    string? Name,
    string? Symbol,
    bool HasImage,
    ProductStockDto? Stock,
    ProductListPriceDto? Price);

public sealed record ProductListPriceDto(
    int Level,
    string Name,
    decimal Gross,
    string? Currency);

public sealed record ProductStockDto(
    int WarehouseId,
    string? WarehouseSymbol,
    string? WarehouseName,
    bool IsMain,
    decimal Quantity,
    decimal Reserved,
    decimal Available,
    decimal? Minimum,
    decimal? Maximum,
    string? Unit);

public sealed record ProductVatDto(
    int Id,
    string? Name,
    string? Symbol,
    decimal? Rate);

public sealed record ProductPriceDto(
    int Level,
    string Name,
    decimal? Net,
    decimal? Gross,
    string? Currency);

public sealed record ProductDetailsDto(
    int Id,
    string? Name,
    string? Symbol,
    string? Description,
    string? Unit,
    string? PrimaryBarcode,
    IReadOnlyList<string> AdditionalBarcodes,
    ProductVatDto? Vat,
    bool HasImage,
    IReadOnlyList<ProductStockDto> Warehouses,
    IReadOnlyList<ProductPriceDto> Prices);

public sealed record ProductImageDto(
    byte[] Content,
    string ContentType);
