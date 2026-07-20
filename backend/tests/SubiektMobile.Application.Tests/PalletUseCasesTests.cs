using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Pallets;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;
using Xunit;

namespace SubiektMobile.Application.Tests;

public sealed class PalletUseCasesTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid CreatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EmployeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OrganizationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Candidates_include_only_packed_quantity_not_already_palletized()
    {
        var order = CreatePublishedOrder();
        var item = order.Items.Single();
        var actor = EmployeeActor();
        var store = new PalletStoreStub(order)
        {
            PalletizedQuantities = new Dictionary<Guid, decimal> { [item.Id] = 4m }
        };
        var handler = new ListPalletCandidatesHandler(store,
            new ApplicationAuthorizationService(new CurrentActorAccessorStub(actor)));

        var result = await handler.Handle(new ListPalletCandidatesQuery(order.Id), CancellationToken.None);

        var candidate = Assert.Single(result.Items);
        Assert.Equal(10m, candidate.PackedQuantity);
        Assert.Equal(4m, candidate.PalletizedQuantity);
        Assert.Equal(6m, candidate.AvailableForPalletQuantity);
    }

    [Fact]
    public async Task Duplicate_operation_with_different_payload_is_rejected()
    {
        var order = CreatePublishedOrder();
        var operationId = Guid.NewGuid();
        var item = order.Items.Single();
        var store = new PalletStoreStub(order)
        {
            Operation = new PalletOperationSnapshot(Guid.NewGuid(), operationId, order.Id, 25m,
                [new PalletOperationItemSnapshot(item.Id, 5m)])
        };
        var handler = new CreatePalletHandler(store, new PalletNumberGenerator(),
            new ApplicationAuthorizationService(new CurrentActorAccessorStub(EmployeeActor())),
            new AuditEntryFactory(), TimeProvider.System);

        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            handler.Handle(new CreatePalletCommand(order.Id, operationId, 25m,
                [new CreatePalletItemInput(item.Id, 6m, item.Version)]), CancellationToken.None));
    }

    [Fact]
    public async Task Employee_not_assigned_to_order_cannot_create_pallet()
    {
        var order = CreatePublishedOrder(EmployeeId: Guid.NewGuid());
        var item = order.Items.Single();
        var handler = new CreatePalletHandler(new PalletStoreStub(order), new PalletNumberGenerator(),
            new ApplicationAuthorizationService(new CurrentActorAccessorStub(EmployeeActor())),
            new AuditEntryFactory(), TimeProvider.System);

        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            handler.Handle(new CreatePalletCommand(order.Id, Guid.NewGuid(), 25m,
                [new CreatePalletItemInput(item.Id, 1m, item.Version)]), CancellationToken.None));
    }

    [Fact]
    public async Task Issuing_label_records_selected_issue_mode_after_rendering_document()
    {
        var actor = EmployeeActor();
        var store = new PalletStoreStub(CreatePublishedOrder()) { Details = PalletDetails() };
        var handler = new IssuePalletLabelHandler(store, new LabelRendererStub(),
            new ApplicationAuthorizationService(new CurrentActorAccessorStub(actor)),
            new AuditEntryFactory(), TimeProvider.System);

        var result = await handler.Handle(new IssuePalletLabelCommand(store.Details.Id,
            PalletLabelIssueMode.Download, PalletLabelLanguage.English), CancellationToken.None);

        Assert.Equal("etykieta-PAL-1.pdf", result.FileName);
        var audit = Assert.Single(store.LabelIssues);
        Assert.Equal("PalletLabelDownloadIssued", audit.Action);
        Assert.Equal(store.Details.Id, audit.TargetId);
    }

    [Fact]
    public async Task Previewing_label_does_not_record_an_issue()
    {
        var store = new PalletStoreStub(CreatePublishedOrder()) { Details = PalletDetails() };
        var handler = new GetPalletLabelPreviewHandler(store, new LabelRendererStub(),
            new ApplicationAuthorizationService(new CurrentActorAccessorStub(EmployeeActor())));

        var result = await handler.Handle(new GetPalletLabelPreviewQuery(store.Details.Id,
            PalletLabelLanguage.Polish),
            CancellationToken.None);

        Assert.Equal("etykieta-PAL-1.pdf", result.FileName);
        Assert.Empty(store.LabelIssues);
    }

    private static WarehouseOrder CreatePublishedOrder(Guid? EmployeeId = null)
    {
        var employeeId = EmployeeId ?? PalletUseCasesTests.EmployeeId;
        var order = WarehouseOrder.Create(Guid.NewGuid(), "ZAM-1", "Customer",
            new DateOnly(2026, 7, 6), CreatorId, "Creator", Now,
            PickingMode.SingleAssignee,
            [new WarehouseOrderAssigneeCandidate(employeeId, OrganizationId, "Picker")]);
        var item = order.AddItem(7, "Test product", "TP", 20m, "szt.", 1.2m,
            CreatorId, "Creator", Now);
        order.Publish(new DateOnly(2026, 7, 5), CreatorId, "Creator", Now);
        order.PackItem(item.Id, 10m, new PickingActor(ActorKind.Employee, employeeId, "Picker"),
            false, Now.AddMinutes(1));
        return order;
    }

    private static CurrentActor EmployeeActor() => new(
        ActorKind.Employee,
        EmployeeId,
        OrganizationId,
        "Picker",
        Permissions.For(ActorKind.Employee),
        Guid.NewGuid());

    private static PalletDetailsDto PalletDetails() => new(Guid.NewGuid(), Guid.NewGuid(), "ZAM-1",
        "PAL-1", "Customer", PalletStatus.Closed, 25m, 10m, 35m,
        ActorKind.Employee, EmployeeId, "Picker", Now,
        [], new PalletLabelPreviewDto("ZAM-1", "PAL-1", "Customer", 10m, 25m, 35m,
            [new PalletLabelItemDto("Product", 1m, "szt.")]), []);

    private sealed class PalletStoreStub(WarehouseOrder order) : IPalletStore
    {
        public IReadOnlyDictionary<Guid, decimal> PalletizedQuantities { get; init; } =
            new Dictionary<Guid, decimal>();
        public PalletOperationSnapshot? Operation { get; init; }
        public PalletDetailsDto? Details { get; init; }
        public List<AuditEntry> LabelIssues { get; } = [];

        public Task<PagedResult<PalletListItemDto>> ListAsync(int page, int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<PalletListItemDto>([], page, pageSize, 0, 0));

        public Task<WarehouseOrder?> FindWarehouseOrderAsync(Guid warehouseOrderId, bool tracking, CancellationToken cancellationToken) =>
            Task.FromResult<WarehouseOrder?>(warehouseOrderId == order.Id ? order : null);

        public Task<IReadOnlyDictionary<Guid, decimal>> GetPalletizedQuantitiesAsync(
            Guid warehouseOrderId, CancellationToken cancellationToken) =>
            Task.FromResult(PalletizedQuantities);

        public Task<PalletOperationSnapshot?> FindOperationAsync(
            Guid operationId, CancellationToken cancellationToken) =>
            Task.FromResult(Operation?.OperationId == operationId ? Operation : null);

        public Task<PalletDetailsDto?> GetDetailsAsync(Guid palletId, CancellationToken cancellationToken) =>
            Task.FromResult(Details?.Id == palletId ? Details : null);

        public Task SaveLabelIssueAsync(AuditEntry audit, CancellationToken cancellationToken)
        {
            LabelIssues.Add(audit);
            return Task.CompletedTask;
        }

        public Task<PalletStoreMutationResult> SaveClosedAsync(Pallet pallet,
            IReadOnlyCollection<PalletItemVersion> expectedItemVersions,
            AuditEntry audit, CancellationToken cancellationToken) =>
            Task.FromResult(PalletStoreMutationResult.Success);
    }

    private sealed class CurrentActorAccessorStub(CurrentActor? actor) : ICurrentActorAccessor
    {
        public CurrentActor? Actor { get; } = actor;
    }

    private sealed class LabelRendererStub : IPalletLabelPdfRenderer
    {
        public byte[] Render(PalletLabelPreviewDto label, PalletLabelLanguage language) => [1, 2, 3];
    }
}
