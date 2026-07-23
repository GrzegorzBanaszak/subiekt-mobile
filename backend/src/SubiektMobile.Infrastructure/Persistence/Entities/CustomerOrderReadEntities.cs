namespace SubiektMobile.Infrastructure.Persistence.Entities;

/// <summary>Read-only projection of a Subiekt GT customer order (ZK).</summary>
public sealed class DokDokument
{
    public int Id { get; set; }
    public int Type { get; set; }
    public string? FullNumber { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? FulfilmentDueAt { get; set; }
    public int? RecipientId { get; set; }
    public string? Notes { get; set; }
    public int Status { get; set; }
}

/// <summary>Read-only projection of an item on a Subiekt GT commercial document.</summary>
public sealed class DokPozycja
{
    public int Id { get; set; }
    public int? CommercialDocumentId { get; set; }
    public int? ProductId { get; set; }
    public int? ProductKind { get; set; }
    public string? Description { get; set; }
    public int? LineNumber { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
}
