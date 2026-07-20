using SubiektMobile.Application.Identity;
using SubiektMobile.Application.WarehouseOrders;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;
using Xunit;

namespace SubiektMobile.Application.Tests;

public sealed class CreateWarehouseOrderUseCaseTests
{
    [Fact]
    public async Task New_order_and_all_items_are_saved_in_one_store_operation()
    {
        var actorId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actor = new CurrentActor(ActorKind.Administrator, actorId, null, "Admin",
            Permissions.For(ActorKind.Administrator), Guid.NewGuid());
        var store = new WarehouseOrderStoreStub();
        var handler = new CreateWarehouseOrderHandler(
            store,
            new NumberGeneratorStub(),
            new WorkforceStub(employeeId, organizationId),
            new ProductRepositoryStub(),
            new ApplicationAuthorizationService(new ActorAccessorStub(actor)),
            new AuditEntryFactory(),
            TimeProvider.System);

        var result = await handler.Handle(new CreateWarehouseOrderCommand(
            "Klient",
            new DateOnly(2026, 7, 20),
            PickingMode.SingleAssignee,
            [employeeId],
            [new CreateWarehouseOrderItemInput(10, 2m), new CreateWarehouseOrderItemInput(20, 3.5m)]),
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.Version);
        Assert.NotNull(store.AddedOrder);
        Assert.Equal(2, store.AddedOrder.Items.Count);
        Assert.Equal(1, store.AddCalls);
    }

    private sealed class WarehouseOrderStoreStub : IWarehouseOrderStore
    {
        public WarehouseOrder? AddedOrder { get; private set; }
        public int AddCalls { get; private set; }

        public Task<WarehouseOrderStoreResult> AddAsync(WarehouseOrder order, AuditEntry audit, CancellationToken cancellationToken)
        {
            AddedOrder = order;
            AddCalls++;
            return Task.FromResult(WarehouseOrderStoreResult.Success);
        }

        public Task<PagedResult<WarehouseOrderListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<WarehouseOrder?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<WarehouseOrderStoreResult> SaveAsync(WarehouseOrder order, long expectedVersion, AuditEntry audit,
            CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<WarehouseOrderStoreResult> DeleteAsync(WarehouseOrder order, long expectedVersion, AuditEntry audit,
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class NumberGeneratorStub : IWarehouseOrderNumberGenerator
    {
        public string Generate(Guid orderId, DateTimeOffset now) => "ZAM-TEST";
    }

    private sealed class WorkforceStub(Guid employeeId, Guid organizationId) : IWarehouseOrderWorkforceDirectory
    {
        public Task<IReadOnlyList<WarehouseOrderAssigneeCandidate>> ResolveActiveAsync(
            IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WarehouseOrderAssigneeCandidate>>(
                [new WarehouseOrderAssigneeCandidate(employeeId, organizationId, "Magazynier")]);

        public Task<IReadOnlyList<AvailableWarehouseOrderAssigneeDto>> ListAvailableAsync(
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class ProductRepositoryStub : IProductReadRepository
    {
        public Task<ProductWarehouseOrderSnapshot?> GetProductWarehouseOrderSnapshotAsync(int id,
            CancellationToken cancellationToken) => Task.FromResult<ProductWarehouseOrderSnapshot?>(
                new ProductWarehouseOrderSnapshot(id, $"Towar {id}", $"T-{id}", "szt.", 1m));
        public Task<PagedResult<ProductListItemDto>> GetProductsAsync(string? search, int page, int pageSize,
            CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductDetailsDto?> GetProductDetailsAsync(int id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<ProductImageDto?> GetProductImageAsync(int id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class ActorAccessorStub(CurrentActor actor) : ICurrentActorAccessor
    {
        public CurrentActor? Actor { get; } = actor;
    }
}
