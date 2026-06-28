using MediatR;

namespace SubiektMobile.Application.Products;

public sealed record GetProductsQuery(
    string? Search,
    int Page,
    int PageSize) : IRequest<PagedResult<ProductListItemDto>>;

public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IProductReadRepository _repository;

    public GetProductsQueryHandler(IProductReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<ProductListItemDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim();

        return _repository.GetProductsAsync(
            search,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}

public sealed record GetProductDetailsQuery(int Id) : IRequest<ProductDetailsDto?>;

public sealed class GetProductDetailsQueryHandler
    : IRequestHandler<GetProductDetailsQuery, ProductDetailsDto?>
{
    private readonly IProductReadRepository _repository;

    public GetProductDetailsQueryHandler(IProductReadRepository repository)
    {
        _repository = repository;
    }

    public Task<ProductDetailsDto?> Handle(
        GetProductDetailsQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetProductDetailsAsync(request.Id, cancellationToken);
    }
}

public sealed record GetProductImageQuery(int Id) : IRequest<ProductImageDto?>;

public sealed class GetProductImageQueryHandler
    : IRequestHandler<GetProductImageQuery, ProductImageDto?>
{
    private readonly IProductReadRepository _repository;

    public GetProductImageQueryHandler(IProductReadRepository repository)
    {
        _repository = repository;
    }

    public Task<ProductImageDto?> Handle(
        GetProductImageQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetProductImageAsync(request.Id, cancellationToken);
    }
}
