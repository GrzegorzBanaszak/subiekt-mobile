using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Api.Models;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;

namespace SubiektMobile.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.CatalogRead)]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ProductListItemResponse>>> GetProducts(
        [FromQuery] GetProductsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetProductsQuery(request.Search, request.Page, request.PageSize),
            cancellationToken);

        var items = result.Items
            .Select(product => new ProductListItemResponse(
                product.Id,
                product.Name,
                product.Symbol,
                BuildImageUrl(product.Id, product.HasImage),
                product.Stock,
                product.Price))
            .ToList();

        return Ok(new PagedResponse<ProductListItemResponse>(
            items,
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(ProductDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsResponse>> GetProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _sender.Send(
            new GetProductDetailsQuery(id),
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(new ProductDetailsResponse(
            product.Id,
            product.Name,
            product.Symbol,
            product.Description,
            product.Unit,
            product.PrimaryBarcode,
            product.AdditionalBarcodes,
            product.Vat,
            BuildImageUrl(product.Id, product.HasImage),
            product.Warehouses,
            product.Prices));
    }

    [HttpGet("{id:int:min(1)}/image")]
    [Produces("image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp", "application/octet-stream")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(
        int id,
        CancellationToken cancellationToken)
    {
        var image = await _sender.Send(
            new GetProductImageQuery(id),
            cancellationToken);

        if (image is null)
        {
            return NotFound();
        }

        return File(image.Content, image.ContentType);
    }

    private static string? BuildImageUrl(int productId, bool hasImage)
    {
        return hasImage
            ? $"/api/products/{productId}/image"
            : null;
    }
}
