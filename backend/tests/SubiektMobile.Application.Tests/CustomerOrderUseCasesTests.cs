using SubiektMobile.Application.CustomerOrders;
using SubiektMobile.Application.Customers;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Application.WarehouseOrders;
using SubiektMobile.Domain.CustomerOrders;
using SubiektMobile.Domain.Customers;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.WarehouseOrders;
using Xunit;

namespace SubiektMobile.Application.Tests;

public sealed class CustomerOrderUseCasesTests
{
    [Fact]
    public async Task Complete_mapping_with_missing_packaging_is_a_warning_and_order_converts_once()
    {
        var now = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();
        var customer = Customer.Create(Guid.NewGuid(), "CUS", "Customer", null, null, null, true, now);
        var site = customer.AddSite(Guid.NewGuid(), "SITE", "Site", "PL", true, now);
        site.ConfigureLogisticsProfile(new CustomerSiteProfileInput("Customer", "Street 1", "00-001", "Warsaw",
            "Dock 1", null, "V-1", null, null, false, false, false, null, VdaLabelProfile.Vda4902), now);
        var order = CustomerOrder.Create(Guid.NewGuid(), customer.Id, site.Id, null, null,
            new DateOnly(2026, 8, 1), null, actorId, "Admin", now);
        order.AddItem(Guid.NewGuid(), "P-100", 2m, actorId, "Admin", now);
        var store = new CustomerOrderStoreStub(order);
        var handler = new CustomerOrderHandlers(store, new CustomerStoreStub(customer, site), new PackagingStoreStub(customer.Id),
            new ProductStoreStub(), new NumberGeneratorStub(), Authorization(actorId), new AuditEntryFactory(),
            new FixedTimeProvider(now));

        var readiness = await handler.Handle(new GetCustomerOrderReadinessQuery(order.Id), CancellationToken.None);
        Assert.True(readiness.CanConvert);
        Assert.Contains(readiness.Issues, x => x.Code == "PackagingMissing"
            && x.Severity == CustomerOrderReadinessSeverity.Warning);

        var ready = await handler.Handle(new MarkCustomerOrderReadyCommand(order.Id, order.Version), CancellationToken.None);
        var converted = await handler.Handle(new ConvertCustomerOrderCommand(order.Id, ready.Version), CancellationToken.None);

        Assert.Equal(CustomerOrderStatus.Converted, converted.CustomerOrder.Status);
        Assert.Equal(order.Id, store.ConvertedWarehouseOrder!.CustomerOrderId);
        Assert.Equal("P-100", Assert.Single(store.ConvertedWarehouseOrder.Items).CustomerPartNumber);
    }

    [Fact]
    public async Task Concurrent_conversion_snapshots_create_only_one_warehouse_order()
    {
        var now = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();
        var customer = Customer.Create(Guid.NewGuid(), "CUS", "Customer", null, null, null, true, now);
        var site = customer.AddSite(Guid.NewGuid(), "SITE", "Site", "PL", true, now);
        site.ConfigureLogisticsProfile(new CustomerSiteProfileInput("Customer", "Street 1", "00-001", "Warsaw",
            "Dock 1", null, "V-1", null, null, false, false, false, null, VdaLabelProfile.Vda4902), now);
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var firstSnapshot = CreateReadyOrder(orderId, itemId, customer.Id, site.Id, actorId, now);
        var secondSnapshot = CreateReadyOrder(orderId, itemId, customer.Id, site.Id, actorId, now);
        var store = new CustomerOrderStoreStub(firstSnapshot, secondSnapshot);
        var handler = new CustomerOrderHandlers(store, new CustomerStoreStub(customer, site), new PackagingStoreStub(customer.Id),
            new ProductStoreStub(), new NumberGeneratorStub(), Authorization(actorId), new AuditEntryFactory(),
            new FixedTimeProvider(now));

        await handler.Handle(new ConvertCustomerOrderCommand(orderId, firstSnapshot.Version), CancellationToken.None);
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            handler.Handle(new ConvertCustomerOrderCommand(orderId, secondSnapshot.Version), CancellationToken.None));

        Assert.Equal(1, store.SuccessfulConversions);
        Assert.NotNull(store.ConvertedWarehouseOrder);
    }

    [Fact]
    public async Task Conversion_requires_warehouse_orders_permission()
    {
        var actorId = Guid.NewGuid();
        var authorization = new ApplicationAuthorizationService(new ActorAccessor(new CurrentActor(
            ActorKind.Administrator, actorId, null, "Admin", [Permissions.CustomerOrdersManage], Guid.NewGuid())));
        var handler = new CustomerOrderHandlers(null!, null!, null!, null!, null!, authorization, null!, null!);

        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            handler.Handle(new ConvertCustomerOrderCommand(Guid.NewGuid(), 1), CancellationToken.None));
    }

    private static CustomerOrder CreateReadyOrder(Guid orderId, Guid itemId, Guid customerId, Guid siteId,
        Guid actorId, DateTimeOffset now)
    {
        var order = CustomerOrder.Create(orderId, customerId, siteId, null, null, new DateOnly(2026, 8, 1), null,
            actorId, "Admin", now);
        order.AddItem(itemId, "P-100", 2m, actorId, "Admin", now);
        order.MarkReady(actorId, "Admin", now);
        return order;
    }

    private static IApplicationAuthorizationService Authorization(Guid actorId) =>
        new ApplicationAuthorizationService(new ActorAccessor(new CurrentActor(ActorKind.Administrator, actorId, null,
            "Admin", Permissions.For(ActorKind.Administrator), Guid.NewGuid())));

    private sealed class CustomerOrderStoreStub(CustomerOrder order, CustomerOrder? concurrentSnapshot = null) : ICustomerOrderStore
    {
        private int _trackedFinds;
        private int _conversionAttempts;
        private int _successfulConversions;
        public WarehouseOrder? ConvertedWarehouseOrder { get; private set; }
        public int SuccessfulConversions => _successfulConversions;
        public Task<PagedResult<CustomerOrderListItemDto>> ListAsync(string? search, CustomerOrderStatus? status, Guid? customerId, Guid? customerSiteId, DateOnly? dueDateFrom, DateOnly? dueDateTo, int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CustomerOrder?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken)
        {
            if (id != order.Id) return Task.FromResult<CustomerOrder?>(null);
            if (tracking && concurrentSnapshot is not null && Interlocked.Increment(ref _trackedFinds) > 1)
                return Task.FromResult<CustomerOrder?>(concurrentSnapshot);
            return Task.FromResult<CustomerOrder?>(order);
        }
        public Task<Guid?> FindWarehouseOrderIdAsync(Guid customerOrderId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(ConvertedWarehouseOrder?.Id);
        public Task<PagedResult<CustomerOrderActivityDto>> ListActivityAsync(Guid customerOrderId, int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CustomerOrderStoreResult> AddAsync(CustomerOrder customerOrder, AuditEntry audit, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CustomerOrderStoreResult> SaveAsync(CustomerOrder customerOrder, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken) => Task.FromResult(CustomerOrderStoreResult.Success);
        public Task<CustomerOrderStoreResult> ConvertAsync(CustomerOrder customerOrder, WarehouseOrder warehouseOrder, long expectedOrderVersion, AuditEntry customerOrderAudit, AuditEntry warehouseOrderAudit, CancellationToken cancellationToken)
        {
            if (concurrentSnapshot is not null && Interlocked.Increment(ref _conversionAttempts) > 1)
                return Task.FromResult(CustomerOrderStoreResult.Conflict);
            ConvertedWarehouseOrder = warehouseOrder;
            Interlocked.Increment(ref _successfulConversions);
            return Task.FromResult(CustomerOrderStoreResult.Success);
        }
    }

    private sealed class CustomerStoreStub(Customer customer, CustomerSite site) : ICustomerStore
    {
        public Task<PagedResult<CustomerListItemDto>> ListAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Customer?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken) => Task.FromResult<Customer?>(id == customer.Id ? customer : null);
        public Task<CustomerSite?> FindSiteAsync(Guid customerId, Guid siteId, bool tracking, CancellationToken cancellationToken) => Task.FromResult<CustomerSite?>(customerId == customer.Id && siteId == site.Id ? site : null);
        public Task<PagedResult<CustomerSiteListItemDto>> ListSitesAsync(Guid customerId, string? search, int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<PagedResult<CustomerActivityDto>> ListActivityAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CustomerStoreResult> AddAsync(Customer customer, AuditEntry audit, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CustomerStoreResult> SaveCustomerAsync(Customer customer, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CustomerStoreResult> SaveSiteAsync(CustomerSite site, long expectedVersion, AuditEntry audit, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class PackagingStoreStub(Guid customerId) : IPackagingStore
    {
        private readonly CustomerPartMappingDto _mapping = new(Guid.NewGuid(), customerId, null, "P-100", 10, "T-10", "Product", null, null, true, 1);
        public Task<CustomerPartResolutionDto> ResolveAsync(Guid requestedCustomerId, Guid? siteId, string partNumber, CancellationToken ct) => Task.FromResult(new CustomerPartResolutionDto(partNumber, CustomerPartReadiness.Mapped, _mapping, null));
        public Task<PagedResult<PackagingTypeDto>> ListPackagingTypesAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct) => throw new NotImplementedException();
        public Task<PackagingType?> FindPackagingTypeAsync(Guid id, bool tracking, CancellationToken ct) => throw new NotImplementedException();
        public Task<PackagingStoreResult> AddPackagingTypeAsync(PackagingType entity, AuditEntry audit, CancellationToken ct) => throw new NotImplementedException();
        public Task<PackagingStoreResult> SavePackagingTypeAsync(PackagingType entity, long version, AuditEntry audit, CancellationToken ct) => throw new NotImplementedException();
        public Task<PagedResult<CustomerPackagingCodeDto>> ListPackagingCodesAsync(Guid customerId, Guid? siteId, int page, int pageSize, CancellationToken ct) => throw new NotImplementedException();
        public Task<CustomerPackagingCode?> FindPackagingCodeAsync(Guid customerId, Guid? siteId, Guid id, bool tracking, CancellationToken ct) => throw new NotImplementedException();
        public Task<PackagingStoreResult> AddPackagingCodeAsync(CustomerPackagingCode entity, AuditEntry audit, CancellationToken ct) => throw new NotImplementedException();
        public Task<PackagingStoreResult> SavePackagingCodeAsync(CustomerPackagingCode entity, long version, AuditEntry audit, CancellationToken ct) => throw new NotImplementedException();
        public Task<PagedResult<CustomerPartMappingDto>> ListPartMappingsAsync(Guid customerId, Guid? siteId, string? search, int page, int pageSize, CancellationToken ct) => throw new NotImplementedException();
        public Task<CustomerPartMapping?> FindPartMappingAsync(Guid customerId, Guid? siteId, Guid id, bool tracking, CancellationToken ct) => throw new NotImplementedException();
        public Task<PackagingStoreResult> AddPartMappingAsync(CustomerPartMapping entity, AuditEntry audit, CancellationToken ct) => throw new NotImplementedException();
        public Task<PackagingStoreResult> SavePartMappingAsync(CustomerPartMapping entity, long version, AuditEntry audit, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class ProductStoreStub : IProductReadRepository
    {
        public Task<ProductWarehouseOrderSnapshot?> GetProductWarehouseOrderSnapshotAsync(int id, CancellationToken cancellationToken) => Task.FromResult<ProductWarehouseOrderSnapshot?>(new(id, "Product", "T-10", "szt.", 1m));
        public Task<PagedResult<ProductListItemDto>> GetProductsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductDetailsDto?> GetProductDetailsAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ProductImageDto?> GetProductImageAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class NumberGeneratorStub : IWarehouseOrderNumberGenerator { public string Generate(Guid warehouseOrderId, DateTimeOffset now) => "ZAM-TEST"; }
    private sealed class ActorAccessor(CurrentActor actor) : ICurrentActorAccessor { public CurrentActor? Actor { get; } = actor; }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
