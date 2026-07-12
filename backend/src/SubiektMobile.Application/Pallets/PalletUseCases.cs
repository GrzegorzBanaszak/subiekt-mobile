using MediatR;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;

namespace SubiektMobile.Application.Pallets;

public sealed record ListPalletCandidatesQuery(Guid OrderId) : IRequest<PalletCandidatesDto>;

public sealed record ListPalletsQuery(int Page, int PageSize) : IRequest<PagedResult<PalletListItemDto>>;

public sealed record GetPalletDetailsQuery(Guid PalletId) : IRequest<PalletDetailsDto>;

public sealed record GetPalletLabelPreviewQuery(Guid PalletId, PalletLabelLanguage Language)
    : IRequest<PalletLabelPdfDto>;

public sealed record IssuePalletLabelCommand(Guid PalletId, PalletLabelIssueMode Mode,
    PalletLabelLanguage Language)
    : IRequest<PalletLabelPdfDto>;

public sealed record CreatePalletCommand(Guid OrderId, Guid OperationId,
    decimal EmptyPalletWeightKg, IReadOnlyCollection<CreatePalletItemInput> Items)
    : IRequest<PalletDetailsDto>;

public sealed class ListPalletsHandler(IPalletStore store,
    IApplicationAuthorizationService authorization)
    : IRequestHandler<ListPalletsQuery, PagedResult<PalletListItemDto>>
{
    public Task<PagedResult<PalletListItemDto>> Handle(ListPalletsQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.PalletsManage);
        return store.ListAsync(Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100), ct);
    }
}

public sealed class ListPalletCandidatesHandler(IPalletStore store,
    IApplicationAuthorizationService authorization)
    : IRequestHandler<ListPalletCandidatesQuery, PalletCandidatesDto>
{
    public async Task<PalletCandidatesDto> Handle(ListPalletCandidatesQuery request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.PalletsManage);
        var order = await FindPublished(request.OrderId, store, false, ct);
        EnsureCanPalletize(order, actor);
        var palletized = await store.GetPalletizedQuantitiesAsync(order.Id, ct);
        var items = order.Items
            .OrderBy(x => x.ProductName)
            .Select(x => Candidate(x, palletized.GetValueOrDefault(x.Id)))
            .Where(x => x.AvailableForPalletQuantity > 0)
            .ToList();
        return new(order.Id, order.Number, order.CustomerName, order.DueDate, items);
    }

    internal static async Task<Order> FindPublished(Guid orderId, IPalletStore store, bool tracking,
        CancellationToken ct)
    {
        var order = await store.FindOrderAsync(orderId, tracking, ct)
            ?? throw new ResourceNotFoundException("Order was not found.");
        if (order.Status != OrderStatus.ReadyForPicking)
            throw new ResourceNotFoundException("Order was not found.");
        return order;
    }

    internal static void EnsureCanPalletize(Order order, CurrentActor actor)
    {
        var isAssignedEmployee = actor.Kind == ActorKind.Employee &&
            order.Assignees.Any(x => x.EmployeeId == actor.Id);
        if (actor.Kind != ActorKind.Administrator && !isAssignedEmployee)
            throw new AccessDeniedException("This employee is not assigned to the order.");
    }

    private static PalletCandidateItemDto Candidate(OrderItem item, decimal palletizedQuantity)
    {
        var packedQuantity = item.PackedQuantity ?? 0;
        var available = Math.Max(0, packedQuantity - palletizedQuantity);
        return new(item.Id, item.ProductId, item.ProductName, item.ProductSymbol, item.Quantity,
            packedQuantity, palletizedQuantity, available, item.Unit, item.UnitWeightKg, item.Version);
    }
}

public sealed class GetPalletDetailsHandler(IPalletStore store,
    IApplicationAuthorizationService authorization)
    : IRequestHandler<GetPalletDetailsQuery, PalletDetailsDto>
{
    public async Task<PalletDetailsDto> Handle(GetPalletDetailsQuery request, CancellationToken ct)
    {
        authorization.Require(Permissions.PalletsManage);
        return await store.GetDetailsAsync(request.PalletId, ct)
            ?? throw new ResourceNotFoundException("Pallet was not found.");
    }
}

public sealed class GetPalletLabelPreviewHandler(IPalletStore store,
    IPalletLabelPdfRenderer renderer, IApplicationAuthorizationService authorization)
    : IRequestHandler<GetPalletLabelPreviewQuery, PalletLabelPdfDto>
{
    public async Task<PalletLabelPdfDto> Handle(GetPalletLabelPreviewQuery request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Language))
            throw new RequestValidationException("A valid label language is required.");
        authorization.Require(Permissions.PalletsManage);
        var pallet = await store.GetDetailsAsync(request.PalletId, ct)
            ?? throw new ResourceNotFoundException("Pallet was not found.");
        return Render(pallet, request.Language, renderer);
    }

    internal static PalletLabelPdfDto Render(PalletDetailsDto pallet, PalletLabelLanguage language,
        IPalletLabelPdfRenderer renderer) =>
        new($"etykieta-{pallet.PalletNumber}.pdf", renderer.Render(pallet.Label, language));
}

public sealed class IssuePalletLabelHandler(IPalletStore store, IPalletLabelPdfRenderer renderer,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<IssuePalletLabelCommand, PalletLabelPdfDto>
{
    public async Task<PalletLabelPdfDto> Handle(IssuePalletLabelCommand request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Mode) || !Enum.IsDefined(request.Language))
            throw new RequestValidationException("A valid label issue mode and language are required.");
        var actor = authorization.Require(Permissions.PalletsManage);
        var pallet = await store.GetDetailsAsync(request.PalletId, ct)
            ?? throw new ResourceNotFoundException("Pallet was not found.");
        var document = GetPalletLabelPreviewHandler.Render(pallet, request.Language, renderer);
        var action = request.Mode == PalletLabelIssueMode.Print
            ? "PalletLabelPrintIssued"
            : "PalletLabelDownloadIssued";
        await store.SaveLabelIssueAsync(audits.Create(actor, action, "Pallet", pallet.Id,
            time.GetUtcNow()), ct);
        return document;
    }
}

public sealed class CreatePalletHandler(IPalletStore store, IPalletNumberGenerator numbers,
    IApplicationAuthorizationService authorization, IAuditEntryFactory audits, TimeProvider time)
    : IRequestHandler<CreatePalletCommand, PalletDetailsDto>
{
    public async Task<PalletDetailsDto> Handle(CreatePalletCommand request, CancellationToken ct)
    {
        var actor = authorization.Require(Permissions.PalletsManage);
        ValidateRequest(request);

        var previous = await store.FindOperationAsync(request.OperationId, ct);
        if (previous is not null)
        {
            EnsureSameOperation(previous, request);
            return await store.GetDetailsAsync(previous.PalletId, ct)
                ?? throw new ResourceConflictException("The pallet operation could not be loaded.");
        }

        var order = await ListPalletCandidatesHandler.FindPublished(request.OrderId, store, true, ct);
        ListPalletCandidatesHandler.EnsureCanPalletize(order, actor);
        var palletized = await store.GetPalletizedQuantitiesAsync(order.Id, ct);
        var itemsById = order.Items.ToDictionary(x => x.Id);
        var allocations = new List<PalletItemAllocation>();
        var expectedVersions = new List<PalletItemVersion>();

        foreach (var input in request.Items)
        {
            if (!itemsById.TryGetValue(input.OrderItemId, out var item))
                throw new ResourceNotFoundException("Order item was not found.");
            if (input.ItemVersion <= 0 || item.Version != input.ItemVersion)
                throw new ResourceConflictException("The item was modified by another request.");

            var alreadyPalletized = palletized.GetValueOrDefault(item.Id);
            var packedQuantity = item.PackedQuantity ?? 0;
            var available = packedQuantity - alreadyPalletized;
            if (input.Quantity > available)
                throw new ResourceConflictException("The item was modified by another request.");
            if (item.UnitWeightKg is null or <= 0)
                throw new RequestValidationException("Every pallet item must have a unit weight greater than zero.");

            allocations.Add(new(item.Id, input.Quantity, item.UnitWeightKg.Value));
            expectedVersions.Add(new(item.Id, input.ItemVersion));
            order.AssignPackedQuantityToPallet(item.Id, alreadyPalletized + input.Quantity);
        }

        var now = time.GetUtcNow();
        var palletId = Guid.NewGuid();
        var pallet = Pallet.CreateClosed(palletId, request.OperationId, order.Id,
            numbers.Generate(palletId, now), request.EmptyPalletWeightKg, allocations,
            new PickingActor(actor.Kind, actor.Id, actor.DisplayName), now);

        var result = await store.SaveClosedAsync(pallet, expectedVersions,
            audits.Create(actor, "PalletClosed", "Pallet", pallet.Id, now), ct);
        if (result == PalletStoreMutationResult.Conflict)
            throw new ResourceConflictException("The item was modified by another request.");
        if (result == PalletStoreMutationResult.DuplicateOperation)
        {
            var duplicated = await store.FindOperationAsync(request.OperationId, ct)
                ?? throw new ResourceConflictException("The pallet operation could not be completed.");
            EnsureSameOperation(duplicated, request);
            return await store.GetDetailsAsync(duplicated.PalletId, ct)
                ?? throw new ResourceConflictException("The pallet operation could not be loaded.");
        }

        return await store.GetDetailsAsync(pallet.Id, ct)
            ?? throw new ResourceConflictException("The pallet operation could not be loaded.");
    }

    private static void ValidateRequest(CreatePalletCommand request)
    {
        if (request.OperationId == Guid.Empty)
            throw new RequestValidationException("operationId is required.");
        if (request.EmptyPalletWeightKg < 0)
            throw new RequestValidationException("Empty pallet weight cannot be negative.");
        if (request.Items.Count == 0)
            throw new RequestValidationException("At least one pallet item is required.");
        if (request.Items.Select(x => x.OrderItemId).Distinct().Count() != request.Items.Count)
            throw new RequestValidationException("An order item can be selected only once.");
        foreach (var item in request.Items)
        {
            if (item.OrderItemId == Guid.Empty || item.ItemVersion <= 0)
                throw new RequestValidationException("Every item must include orderItemId and a positive itemVersion.");
            if (decimal.Round(item.Quantity, 4) != item.Quantity || item.Quantity <= 0)
                throw new RequestValidationException(
                    "Every item quantity must contain at most four decimal places and be greater than zero.");
        }
    }

    private static void EnsureSameOperation(PalletOperationSnapshot previous, CreatePalletCommand request)
    {
        if (previous.OrderId != request.OrderId ||
            previous.EmptyPalletWeightKg != Pallet.NormalizeWeight(request.EmptyPalletWeightKg) ||
            previous.Items.Count != request.Items.Count)
            throw new ResourceConflictException("operationId has already been used for another operation.");

        var previousItems = previous.Items.OrderBy(x => x.OrderItemId).ToList();
        var requestItems = request.Items.OrderBy(x => x.OrderItemId).ToList();
        for (var i = 0; i < previousItems.Count; i++)
        {
            if (previousItems[i].OrderItemId != requestItems[i].OrderItemId ||
                previousItems[i].Quantity != requestItems[i].Quantity)
                throw new ResourceConflictException("operationId has already been used for another operation.");
        }
    }
}

public sealed class PalletNumberGenerator : IPalletNumberGenerator
{
    public string Generate(Guid palletId, DateTimeOffset now) =>
        $"PAL-{now:yyyyMMdd}-{palletId.ToString("N")[..8].ToUpperInvariant()}";
}
