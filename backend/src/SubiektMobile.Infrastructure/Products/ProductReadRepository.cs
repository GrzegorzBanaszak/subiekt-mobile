using Microsoft.EntityFrameworkCore;
using SubiektMobile.Application.Products;
using SubiektMobile.Application.Orders;
using SubiektMobile.Infrastructure.Persistence;

namespace SubiektMobile.Infrastructure.Products;

public sealed class ProductReadRepository : IProductReadRepository
{
    private readonly SubiektDbContext _dbContext;

    public ProductReadRepository(SubiektDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductOrderSnapshot?> GetProductOrderSnapshotAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.Towary.AsNoTracking()
            .Where(x => x.Id == id && x.Usuniety != true && x.Zablokowany != true &&
                x.Nazwa != null && x.Nazwa != "" && x.JednMiary != null && x.JednMiary != "")
            .Select(x => new ProductOrderSnapshot(x.Id, x.Nazwa!, x.Symbol, x.JednMiary!, null))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Towary
            .AsNoTracking()
            .Where(product => product.Usuniety != true && product.Zablokowany != true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(product =>
                (product.Nazwa != null && product.Nazwa.Contains(search)) ||
                (product.Symbol != null && product.Symbol.Contains(search)) ||
                (product.KodTowaru != null && product.KodTowaru.Contains(search)) ||
                (product.PodstKodKresk != null && product.PodstKodKresk.Contains(search)) ||
                _dbContext.KodyKreskowe.Any(barcode =>
                    barcode.TowarId == product.Id &&
                    barcode.Kod != null &&
                    barcode.Kod.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var recordsToSkip = ((long)page - 1) * pageSize;

        if (totalCount == 0 || recordsToSkip >= totalCount)
        {
            return new PagedResult<ProductListItemDto>(
                [],
                page,
                pageSize,
                totalCount,
                CalculateTotalPages(totalCount, pageSize));
        }

        var pageRows = await query
            .OrderBy(product => product.Nazwa)
            .ThenBy(product => product.Id)
            .Skip((int)recordsToSkip)
            .Take(pageSize)
            .Select(product => new ProductPageRow
            {
                Id = product.Id,
                Name = product.Nazwa,
                Symbol = product.Symbol,
                Unit = product.JednMiary
            })
            .ToListAsync(cancellationToken);

        var productIds = pageRows.Select(product => product.Id).ToArray();

        var mainWarehouse = await _dbContext.Magazyny
            .AsNoTracking()
            .Where(warehouse => warehouse.Glowny == true)
            .OrderBy(warehouse => warehouse.Id)
            .Select(warehouse => new WarehouseRow
            {
                Id = warehouse.Id,
                Symbol = warehouse.Symbol,
                Name = warehouse.Nazwa,
                IsMain = true
            })
            .FirstOrDefaultAsync(cancellationToken);

        var stockByProduct = new Dictionary<int, StockRow>();
        if (mainWarehouse is not null)
        {
            stockByProduct = await _dbContext.StanyTowarow
                .AsNoTracking()
                .Where(stock =>
                    stock.MagazynId == mainWarehouse.Id &&
                    productIds.Contains(stock.TowarId))
                .Select(stock => new StockRow
                {
                    ProductId = stock.TowarId,
                    Quantity = stock.Stan,
                    Reserved = stock.StanRezerwacji,
                    Minimum = stock.StanMin,
                    Maximum = stock.StanMax
                })
                .ToDictionaryAsync(stock => stock.ProductId, cancellationToken);
        }

        var productsWithImages = await _dbContext.ZdjeciaTowarow
            .AsNoTracking()
            .Where(image =>
                productIds.Contains(image.TowarId) &&
                image.Zdjecie != null)
            .Select(image => image.TowarId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        var items = new List<ProductListItemDto>(pageRows.Count);

        foreach (var product in pageRows)
        {
            ProductStockDto? stock = null;
            if (mainWarehouse is not null)
            {
                stockByProduct.TryGetValue(product.Id, out var stockRow);
                var quantity = stockRow?.Quantity ?? 0m;
                var reserved = stockRow?.Reserved ?? 0m;

                stock = new ProductStockDto(
                    mainWarehouse.Id,
                    mainWarehouse.Symbol,
                    mainWarehouse.Name,
                    true,
                    quantity,
                    reserved,
                    quantity - reserved,
                    stockRow?.Minimum,
                    stockRow?.Maximum,
                    product.Unit);
            }

            items.Add(new ProductListItemDto(
                product.Id,
                product.Name,
                product.Symbol,
                product.Unit,
                productsWithImages.Contains(product.Id),
                stock));
        }

        return new PagedResult<ProductListItemDto>(
            items,
            page,
            pageSize,
            totalCount,
            CalculateTotalPages(totalCount, pageSize));
    }

    public async Task<ProductDetailsDto?> GetProductDetailsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Towary
            .AsNoTracking()
            .Where(item =>
                item.Id == id &&
                item.Usuniety != true &&
                item.Zablokowany != true)
            .Select(item => new ProductDetailsRow
            {
                Id = item.Id,
                Name = item.Nazwa,
                Symbol = item.Symbol,
                Description = item.Opis,
                Unit = item.JednMiary,
                PrimaryBarcode = item.PodstKodKresk,
                VatId = item.IdVatSp
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return null;
        }

        var additionalBarcodes = await _dbContext.KodyKreskowe
            .AsNoTracking()
            .Where(barcode => barcode.TowarId == id && barcode.Kod != null)
            .OrderBy(barcode => barcode.Kod)
            .Select(barcode => barcode.Kod!)
            .ToListAsync(cancellationToken);

        ProductVatDto? vat = null;
        if (product.VatId.HasValue)
        {
            vat = await _dbContext.StawkiVat
                .AsNoTracking()
                .Where(rate => rate.Id == product.VatId.Value)
                .Select(rate => new ProductVatDto(
                    rate.Id,
                    rate.Nazwa,
                    rate.Symbol,
                    rate.Stawka))
                .FirstOrDefaultAsync(cancellationToken);
        }

        var warehouses = await (
                from warehouse in _dbContext.Magazyny.AsNoTracking()
                join stock in _dbContext.StanyTowarow
                        .AsNoTracking()
                        .Where(stock => stock.TowarId == id)
                    on warehouse.Id equals stock.MagazynId into warehouseStocks
                from stock in warehouseStocks.DefaultIfEmpty()
                orderby warehouse.Glowny == true descending, warehouse.Nazwa, warehouse.Id
                select new ProductStockDto(
                    warehouse.Id,
                    warehouse.Symbol,
                    warehouse.Nazwa,
                    warehouse.Glowny == true,
                    stock.Stan ?? 0m,
                    stock.StanRezerwacji ?? 0m,
                    (stock.Stan ?? 0m) - (stock.StanRezerwacji ?? 0m),
                    stock.StanMin,
                    stock.StanMax,
                    product.Unit))
            .ToListAsync(cancellationToken);

        var priceValues = await _dbContext.Ceny
            .AsNoTracking()
            .Where(price => price.IdTowar == id)
            .Select(price => new PriceValuesRow
            {
                Net1 = price.CenaNetto1,
                Net2 = price.CenaNetto2,
                Net3 = price.CenaNetto3,
                Net4 = price.CenaNetto4,
                Net5 = price.CenaNetto5,
                Net6 = price.CenaNetto6,
                Net7 = price.CenaNetto7,
                Net8 = price.CenaNetto8,
                Net9 = price.CenaNetto9,
                Net10 = price.CenaNetto10,
                Gross1 = price.CenaBrutto1,
                Gross2 = price.CenaBrutto2,
                Gross3 = price.CenaBrutto3,
                Gross4 = price.CenaBrutto4,
                Gross5 = price.CenaBrutto5,
                Gross6 = price.CenaBrutto6,
                Gross7 = price.CenaBrutto7,
                Gross8 = price.CenaBrutto8,
                Gross9 = price.CenaBrutto9,
                Gross10 = price.CenaBrutto10,
                Currency1 = price.IdWaluta1,
                Currency2 = price.IdWaluta2,
                Currency3 = price.IdWaluta3,
                Currency4 = price.IdWaluta4,
                Currency5 = price.IdWaluta5,
                Currency6 = price.IdWaluta6,
                Currency7 = price.IdWaluta7,
                Currency8 = price.IdWaluta8,
                Currency9 = price.IdWaluta9,
                Currency10 = price.IdWaluta10
            })
            .FirstOrDefaultAsync(cancellationToken);

        var prices = Array.Empty<ProductPriceDto>();
        if (priceValues is not null)
        {
            var priceNames = await GetPriceNamesAsync(cancellationToken);
            prices = CreatePrices(priceValues, priceNames);
        }

        var hasImage = await _dbContext.ZdjeciaTowarow
            .AsNoTracking()
            .AnyAsync(
                image => image.TowarId == id && image.Zdjecie != null,
                cancellationToken);

        return new ProductDetailsDto(
            product.Id,
            product.Name,
            product.Symbol,
            product.Description,
            product.Unit,
            product.PrimaryBarcode,
            additionalBarcodes,
            vat,
            hasImage,
            warehouses,
            prices);
    }

    public async Task<ProductImageDto?> GetProductImageAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var image = await _dbContext.ZdjeciaTowarow
            .AsNoTracking()
            .Where(item =>
                item.TowarId == id &&
                item.Zdjecie != null &&
                _dbContext.Towary.Any(product =>
                    product.Id == item.TowarId &&
                    product.Usuniety != true &&
                    product.Zablokowany != true))
            .OrderByDescending(item => item.Glowne == true)
            .ThenBy(item => item.Id)
            .Select(item => item.Zdjecie)
            .FirstOrDefaultAsync(cancellationToken);

        if (image is null || image.Length == 0)
        {
            return null;
        }

        return new ProductImageDto(image, DetectContentType(image));
    }

    private async Task<PriceNamesRow?> GetPriceNamesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ParametryTowarow
            .AsNoTracking()
            .OrderBy(parameter => parameter.Id)
            .Select(parameter => new PriceNamesRow
            {
                Name1 = parameter.NazwaCeny1,
                Name2 = parameter.NazwaCeny2,
                Name3 = parameter.NazwaCeny3,
                Name4 = parameter.NazwaCeny4,
                Name5 = parameter.NazwaCeny5,
                Name6 = parameter.NazwaCeny6,
                Name7 = parameter.NazwaCeny7,
                Name8 = parameter.NazwaCeny8,
                Name9 = parameter.NazwaCeny9,
                Name10 = parameter.NazwaCeny10
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ProductPriceDto[] CreatePrices(
        PriceValuesRow values,
        PriceNamesRow? names)
    {
        return
        [
            new(1, GetPriceName(names?.Name1, 1), values.Net1, values.Gross1, values.Currency1?.Trim()),
            new(2, GetPriceName(names?.Name2, 2), values.Net2, values.Gross2, values.Currency2?.Trim()),
            new(3, GetPriceName(names?.Name3, 3), values.Net3, values.Gross3, values.Currency3?.Trim()),
            new(4, GetPriceName(names?.Name4, 4), values.Net4, values.Gross4, values.Currency4?.Trim()),
            new(5, GetPriceName(names?.Name5, 5), values.Net5, values.Gross5, values.Currency5?.Trim()),
            new(6, GetPriceName(names?.Name6, 6), values.Net6, values.Gross6, values.Currency6?.Trim()),
            new(7, GetPriceName(names?.Name7, 7), values.Net7, values.Gross7, values.Currency7?.Trim()),
            new(8, GetPriceName(names?.Name8, 8), values.Net8, values.Gross8, values.Currency8?.Trim()),
            new(9, GetPriceName(names?.Name9, 9), values.Net9, values.Gross9, values.Currency9?.Trim()),
            new(10, GetPriceName(names?.Name10, 10), values.Net10, values.Gross10, values.Currency10?.Trim())
        ];
    }

    private static string GetPriceName(string? configuredName, int level)
    {
        return string.IsNullOrWhiteSpace(configuredName)
            ? $"Cena {level}"
            : configuredName.Trim();
    }

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        return totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    private static string DetectContentType(byte[] content)
    {
        if (content.Length >= 3 &&
            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
        {
            return "image/png";
        }

        if (content.Length >= 6 &&
            content[0] == 0x47 && content[1] == 0x49 && content[2] == 0x46 &&
            content[3] == 0x38 && (content[4] == 0x37 || content[4] == 0x39) && content[5] == 0x61)
        {
            return "image/gif";
        }

        if (content.Length >= 2 && content[0] == 0x42 && content[1] == 0x4D)
        {
            return "image/bmp";
        }

        if (content.Length >= 12 &&
            content[0] == 0x52 && content[1] == 0x49 && content[2] == 0x46 && content[3] == 0x46 &&
            content[8] == 0x57 && content[9] == 0x45 && content[10] == 0x42 && content[11] == 0x50)
        {
            return "image/webp";
        }

        return "application/octet-stream";
    }

    private class ProductPageRow
    {
        public int Id { get; init; }
        public string? Name { get; init; }
        public string? Symbol { get; init; }
        public string? Unit { get; init; }
    }

    private sealed class ProductDetailsRow : ProductPageRow
    {
        public string? Description { get; init; }
        public string? PrimaryBarcode { get; init; }
        public int? VatId { get; init; }
    }

    private sealed class WarehouseRow
    {
        public int Id { get; init; }
        public string? Symbol { get; init; }
        public string? Name { get; init; }
        public bool IsMain { get; init; }
    }

    private sealed class StockRow
    {
        public int ProductId { get; init; }
        public decimal? Quantity { get; init; }
        public decimal? Reserved { get; init; }
        public decimal? Minimum { get; init; }
        public decimal? Maximum { get; init; }
    }

    private sealed class PriceNamesRow
    {
        public string? Name1 { get; init; }
        public string? Name2 { get; init; }
        public string? Name3 { get; init; }
        public string? Name4 { get; init; }
        public string? Name5 { get; init; }
        public string? Name6 { get; init; }
        public string? Name7 { get; init; }
        public string? Name8 { get; init; }
        public string? Name9 { get; init; }
        public string? Name10 { get; init; }
    }

    private sealed class PriceValuesRow
    {
        public decimal? Net1 { get; init; }
        public decimal? Net2 { get; init; }
        public decimal? Net3 { get; init; }
        public decimal? Net4 { get; init; }
        public decimal? Net5 { get; init; }
        public decimal? Net6 { get; init; }
        public decimal? Net7 { get; init; }
        public decimal? Net8 { get; init; }
        public decimal? Net9 { get; init; }
        public decimal? Net10 { get; init; }
        public decimal? Gross1 { get; init; }
        public decimal? Gross2 { get; init; }
        public decimal? Gross3 { get; init; }
        public decimal? Gross4 { get; init; }
        public decimal? Gross5 { get; init; }
        public decimal? Gross6 { get; init; }
        public decimal? Gross7 { get; init; }
        public decimal? Gross8 { get; init; }
        public decimal? Gross9 { get; init; }
        public decimal? Gross10 { get; init; }
        public string? Currency1 { get; init; }
        public string? Currency2 { get; init; }
        public string? Currency3 { get; init; }
        public string? Currency4 { get; init; }
        public string? Currency5 { get; init; }
        public string? Currency6 { get; init; }
        public string? Currency7 { get; init; }
        public string? Currency8 { get; init; }
        public string? Currency9 { get; init; }
        public string? Currency10 { get; init; }
    }
}
