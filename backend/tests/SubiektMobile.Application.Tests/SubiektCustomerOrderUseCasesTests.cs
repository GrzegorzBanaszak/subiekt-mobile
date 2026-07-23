using SubiektMobile.Application.CustomerOrders;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Application.WarehouseOrders;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;
using Xunit;

namespace SubiektMobile.Application.Tests;

public sealed class SubiektCustomerOrderUseCasesTests
{
    [Fact]
    public async Task Conversion_copies_the_read_only_Subiekt_order_and_is_idempotent()
    {
        var now = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);
        var source = new SubiektCustomerOrderDto(42, "ZK 42/2026", "Odbiorca", new DateOnly(2026, 8, 1),
            new DateOnly(2026, 7, 23), 7, null, null,
            [new SubiektCustomerOrderItemDto(99, 10, "Towar", "T-10", 3m, "szt.")]);
        var warehouseOrders = new WarehouseOrderStoreStub();
        var handler = new SubiektCustomerOrderHandlers(new SourceStoreStub(source), warehouseOrders,
            new NumberGeneratorStub(), new ProductStoreStub(), Authorization(), new AuditEntryFactory(), new FixedTimeProvider(now));

        var created = await handler.Handle(new ConvertSubiektCustomerOrderCommand(42), CancellationToken.None);
        var repeated = await handler.Handle(new ConvertSubiektCustomerOrderCommand(42), CancellationToken.None);

        Assert.False(created.WasAlreadyConverted);
        Assert.True(repeated.WasAlreadyConverted);
        Assert.Equal(created.WarehouseOrderId, repeated.WarehouseOrderId);
        Assert.Equal(42, warehouseOrders.Order!.SubiektSourceDocumentId);
        Assert.Equal(99, Assert.Single(warehouseOrders.Order.Items).SubiektSourceItemId);
    }

    private static IApplicationAuthorizationService Authorization()
    {
        var actor = new CurrentActor(ActorKind.Administrator, Guid.NewGuid(), null, "Admin",
            [Permissions.CustomerOrdersManage, Permissions.WarehouseOrdersManage], Guid.NewGuid());
        return new ApplicationAuthorizationService(new ActorAccessor(actor));
    }

    private sealed class SourceStoreStub(SubiektCustomerOrderDto source) : ISubiektCustomerOrderReadRepository
    {
        public Task<PagedResult<SubiektCustomerOrderListItemDto>> ListAsync(string? search, bool includeCompleted, int page,
            int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<SubiektCustomerOrderDto?> FindAsync(int sourceDocumentId, CancellationToken cancellationToken) =>
            Task.FromResult<SubiektCustomerOrderDto?>(sourceDocumentId == source.SourceDocumentId ? source : null);
    }

    private sealed class WarehouseOrderStoreStub : IWarehouseOrderStore
    {
        public WarehouseOrder? Order { get; private set; }
        public Task<PagedResult<WarehouseOrderListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<WarehouseOrder?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken) => Task.FromResult<WarehouseOrder?>(Order?.Id == id ? Order : null);
        public Task<(Guid Id, string Number)?> FindBySubiektSourceDocumentIdAsync(int sourceDocumentId, CancellationToken cancellationToken) => Task.FromResult<(Guid Id, string Number)?>(Order?.SubiektSourceDocumentId == sourceDocumentId ? (Order.Id, Order.Number) : null);
        public Task<WarehouseOrderStoreResult> AddAsync(WarehouseOrder warehouseOrder, AuditEntry audit, CancellationToken cancellationToken) { if (Order is not null) return Task.FromResult(WarehouseOrderStoreResult.Conflict); Order = warehouseOrder; return Task.FromResult(WarehouseOrderStoreResult.Success); }
        public Task<WarehouseOrderStoreResult> SaveAsync(WarehouseOrder warehouseOrder, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<WarehouseOrderStoreResult> DeleteAsync(WarehouseOrder warehouseOrder, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class ProductStoreStub : IProductReadRepository
    {
        public Task<ProductWarehouseOrderSnapshot?> GetProductWarehouseOrderSnapshotAsync(int id, CancellationToken cancellationToken) => Task.FromResult<ProductWarehouseOrderSnapshot?>(new(id, "Towar", "T-10", "szt.", 1m));
        public Task<PagedResult<ProductListItemDto>> GetProductsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductDetailsDto?> GetProductDetailsAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductImageDto?> GetProductImageAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class NumberGeneratorStub : IWarehouseOrderNumberGenerator { public string Generate(Guid warehouseOrderId, DateTimeOffset now) => "ZAM-SUBIEKT"; }
    private sealed class ActorAccessor(CurrentActor actor) : ICurrentActorAccessor { public CurrentActor? Actor { get; } = actor; }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
