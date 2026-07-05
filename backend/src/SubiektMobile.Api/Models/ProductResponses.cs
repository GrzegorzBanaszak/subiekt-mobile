using System.ComponentModel.DataAnnotations;
using SubiektMobile.Application.Products;

namespace SubiektMobile.Api.Models;

public sealed class GetProductsRequest
{
    [StringLength(100)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ProductListItemResponse(
    int Id,
    string? Name,
    string? Symbol,
    string? Unit,
    string? ImageUrl,
    ProductStockDto? Stock);

public sealed record ProductDetailsResponse(
    int Id,
    string? Name,
    string? Symbol,
    string? Description,
    string? Unit,
    string? PrimaryBarcode,
    IReadOnlyList<string> AdditionalBarcodes,
    ProductVatDto? Vat,
    string? ImageUrl,
    IReadOnlyList<ProductStockDto> Warehouses,
    IReadOnlyList<ProductPriceDto> Prices);
