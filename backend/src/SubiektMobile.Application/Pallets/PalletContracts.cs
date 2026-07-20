using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;
using SubiektMobile.Application.Products;

namespace SubiektMobile.Application.Pallets;

public sealed record PalletCandidateItemDto(Guid WarehouseOrderItemId, int ProductId,
    string ProductName, string? ProductSymbol, decimal OrderedQuantity,
    decimal PackedQuantity, decimal PalletizedQuantity, decimal AvailableForPalletQuantity,
    string Unit, decimal? UnitWeightKg, long Version);

public sealed record PalletCandidatesDto(Guid WarehouseOrderId, string WarehouseOrderNumber,
    string CustomerName, DateOnly DueDate, IReadOnlyList<PalletCandidateItemDto> Items);

public sealed record PalletListItemDto(Guid Id, Guid WarehouseOrderId, string WarehouseOrderNumber,
    string PalletNumber, string CustomerName, PalletStatus Status,
    decimal GoodsWeightKg, decimal EmptyPalletWeightKg, decimal TotalWeightKg,
    int ItemCount, DateTimeOffset ClosedAtUtc);

public sealed record CreatePalletItemInput(Guid WarehouseOrderItemId, decimal Quantity, long ItemVersion);

public sealed record PalletDetailsItemDto(Guid WarehouseOrderItemId, int ProductId,
    string ProductName, string? ProductSymbol, decimal Quantity, string Unit,
    decimal UnitWeightKg, decimal LineWeightKg);

public sealed record PalletLabelItemDto(string ProductName, decimal Quantity, string Unit);

public sealed record PalletLabelPreviewDto(string WarehouseOrderNumber, string PalletNumber,
    string CustomerName, decimal GoodsWeightKg, decimal EmptyPalletWeightKg,
    decimal TotalWeightKg, IReadOnlyList<PalletLabelItemDto> Items);

public enum PalletLabelIssueMode
{
    Print,
    Download
}

public enum PalletLabelLanguage
{
    Polish,
    English
}

public sealed record PalletLabelIssueDto(int Number, PalletLabelIssueMode Mode,
    ActorKind ActorKind, string ActorDisplayName, DateTimeOffset OccurredAtUtc);

public sealed record PalletLabelPdfDto(string FileName, byte[] Content);

public sealed record PalletDetailsDto(Guid Id, Guid WarehouseOrderId, string WarehouseOrderNumber,
    string PalletNumber, string CustomerName, PalletStatus Status,
    decimal EmptyPalletWeightKg, decimal GoodsWeightKg, decimal TotalWeightKg,
    ActorKind ClosedByKind, Guid ClosedById, string ClosedByName,
    DateTimeOffset ClosedAtUtc, IReadOnlyList<PalletDetailsItemDto> Items,
    PalletLabelPreviewDto Label, IReadOnlyList<PalletLabelIssueDto> LabelIssues);

public sealed record PalletOperationItemSnapshot(Guid WarehouseOrderItemId, decimal Quantity);

public sealed record PalletOperationSnapshot(Guid PalletId, Guid OperationId,
    Guid WarehouseOrderId, decimal EmptyPalletWeightKg,
    IReadOnlyList<PalletOperationItemSnapshot> Items);

public sealed record PalletItemVersion(Guid WarehouseOrderItemId, long Version);

public enum PalletStoreMutationResult
{
    Success,
    Conflict,
    DuplicateOperation
}

public interface IPalletStore
{
    Task<PagedResult<PalletListItemDto>> ListAsync(int page, int pageSize,
        CancellationToken cancellationToken);
    Task<WarehouseOrder?> FindWarehouseOrderAsync(Guid warehouseOrderId, bool tracking, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, decimal>> GetPalletizedQuantitiesAsync(
        Guid warehouseOrderId, CancellationToken cancellationToken);
    Task<PalletOperationSnapshot?> FindOperationAsync(
        Guid operationId, CancellationToken cancellationToken);
    Task<PalletDetailsDto?> GetDetailsAsync(Guid palletId, CancellationToken cancellationToken);
    Task SaveLabelIssueAsync(AuditEntry audit, CancellationToken cancellationToken);
    Task<PalletStoreMutationResult> SaveClosedAsync(Pallet pallet,
        IReadOnlyCollection<PalletItemVersion> expectedItemVersions,
        AuditEntry audit, CancellationToken cancellationToken);
}

public interface IPalletNumberGenerator
{
    string Generate(Guid palletId, DateTimeOffset now);
}

public interface IPalletLabelPdfRenderer
{
    byte[] Render(PalletLabelPreviewDto label, PalletLabelLanguage language);
}
