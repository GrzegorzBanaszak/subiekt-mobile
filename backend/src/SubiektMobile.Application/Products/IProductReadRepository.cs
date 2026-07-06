namespace SubiektMobile.Application.Products;

public interface IProductReadRepository
{
    Task<Orders.ProductOrderSnapshot?> GetProductOrderSnapshotAsync(
        int id,
        CancellationToken cancellationToken);

    Task<PagedResult<ProductListItemDto>> GetProductsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ProductDetailsDto?> GetProductDetailsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ProductImageDto?> GetProductImageAsync(
        int id,
        CancellationToken cancellationToken);
}
