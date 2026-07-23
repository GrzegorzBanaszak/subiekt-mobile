using Microsoft.EntityFrameworkCore;
using SubiektMobile.Application.CustomerOrders;
using SubiektMobile.Application.Products;
using SubiektMobile.Infrastructure.Persistence;

namespace SubiektMobile.Infrastructure.CustomerOrders;

public sealed class SubiektCustomerOrderReadRepository(SubiektDbContext db) : ISubiektCustomerOrderReadRepository
{
    private const int CustomerOrderDocumentType = 16; // ZK — zamówienie od klienta
    private const int ProductKind = 1;

    public async Task<PagedResult<SubiektCustomerOrderListItemDto>> ListAsync(string? search, bool includeCompleted,
        int page, int pageSize, CancellationToken ct)
    {
        var query = from document in db.DokumentyHandlowe.AsNoTracking()
            join contractor in db.Kontrahenci.AsNoTracking() on document.RecipientId equals contractor.Id into contractors
            from contractor in contractors.DefaultIfEmpty()
            join address in db.Adresy.AsNoTracking().Where(x => x.AddressType == 1)
                on contractor.Id equals address.ObjectId into addresses
            from address in addresses.DefaultIfEmpty()
            where document.Type == CustomerOrderDocumentType && (includeCompleted || document.Status != 8)
            select new SourceOrderRow
            {
                Id = document.Id,
                Number = document.FullNumber,
                CustomerName = address.FullName ?? address.Name ?? contractor.Symbol,
                RequestedDeliveryAt = document.FulfilmentDueAt ?? document.IssuedAt,
                Status = document.Status
            };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var phrase = search.Trim();
            query = query.Where(x => (x.Number ?? "").Contains(phrase) || (x.CustomerName ?? "").Contains(phrase));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.RequestedDeliveryAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var ids = rows.Select(x => x.Id).ToArray();
        var itemCounts = await db.PozycjeDokumentow.AsNoTracking()
            .Where(x => x.CommercialDocumentId != null && ids.Contains(x.CommercialDocumentId.Value)
                && x.ProductKind == ProductKind && x.ProductId != null && x.Quantity > 0)
            .GroupBy(x => x.CommercialDocumentId!.Value).Select(x => new { DocumentId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.DocumentId, x => x.Count, ct);
        var items = rows.Select(x => new SubiektCustomerOrderListItemDto(x.Id, Number(x.Number, x.Id),
            CustomerName(x.CustomerName, x.Id), Date(x.RequestedDeliveryAt), x.Status,
            itemCounts.GetValueOrDefault(x.Id), null)).ToList();
        return new PagedResult<SubiektCustomerOrderListItemDto>(items, page, pageSize, total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<SubiektCustomerOrderDto?> FindAsync(int sourceDocumentId, CancellationToken ct)
    {
        var header = await (from document in db.DokumentyHandlowe.AsNoTracking()
            join contractor in db.Kontrahenci.AsNoTracking() on document.RecipientId equals contractor.Id into contractors
            from contractor in contractors.DefaultIfEmpty()
            join address in db.Adresy.AsNoTracking().Where(x => x.AddressType == 1)
                on contractor.Id equals address.ObjectId into addresses
            from address in addresses.DefaultIfEmpty()
            where document.Id == sourceDocumentId && document.Type == CustomerOrderDocumentType
            select new SourceOrderRow
            {
                Id = document.Id,
                Number = document.FullNumber,
                CustomerName = address.FullName ?? address.Name ?? contractor.Symbol,
                IssuedAt = document.IssuedAt,
                RequestedDeliveryAt = document.FulfilmentDueAt ?? document.IssuedAt,
                Status = document.Status,
                Notes = document.Notes
            }).SingleOrDefaultAsync(ct);
        if (header is null) return null;

        var items = await (from item in db.PozycjeDokumentow.AsNoTracking()
            join product in db.Towary.AsNoTracking() on item.ProductId equals product.Id
            where item.CommercialDocumentId == sourceDocumentId && item.ProductKind == ProductKind
                && item.ProductId != null && item.Quantity > 0
            orderby item.LineNumber, item.Id
            select new SubiektCustomerOrderItemDto(item.Id, item.ProductId!.Value,
                item.Description ?? product.Nazwa ?? product.Symbol ?? item.ProductId.Value.ToString(), product.Symbol,
                item.Quantity!.Value, item.Unit ?? product.JednMiary ?? "szt."))
            .ToListAsync(ct);
        return new SubiektCustomerOrderDto(header.Id, Number(header.Number, header.Id), CustomerName(header.CustomerName, header.Id),
            Date(header.RequestedDeliveryAt), Date(header.IssuedAt ?? header.RequestedDeliveryAt), header.Status,
            Text(header.Notes), null, items);
    }

    private static string Number(string? value, int id) => string.IsNullOrWhiteSpace(value) ? $"ZK/{id}" : value.Trim();
    private static string CustomerName(string? value, int id) => string.IsNullOrWhiteSpace(value) ? $"Kontrahent {id}" : value.Trim();
    private static DateOnly Date(DateTime? value) => DateOnly.FromDateTime(value ?? DateTime.UtcNow.Date);
    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class SourceOrderRow
    {
        public int Id { get; init; }
        public string? Number { get; init; }
        public string? CustomerName { get; init; }
        public DateTime? IssuedAt { get; init; }
        public DateTime? RequestedDeliveryAt { get; init; }
        public int Status { get; init; }
        public string? Notes { get; init; }
    }
}
