using SubiektMobile.Application.Customers;
using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Customers;
using SubiektMobile.Domain.Identity;
using Xunit;

namespace SubiektMobile.Application.Tests;

public sealed class CustomerUseCaseTests
{
    [Fact]
    public async Task Creating_customer_validates_subiekt_reference_and_writes_an_audit_entry()
    {
        var actor = new CurrentActor(ActorKind.Administrator, Guid.NewGuid(), null, "Administrator",
            Permissions.For(ActorKind.Administrator), Guid.NewGuid());
        var store = new CustomerStoreStub();
        var handler = new CreateCustomerHandler(store, new ContractorDirectoryStub(true),
            new ApplicationAuthorizationService(new ActorAccessorStub(actor)), new AuditEntryFactory(), TimeProvider.System);

        var result = await handler.Handle(new CreateCustomerCommand("KRM", "Kramp", "PL123", 42, null, true),
            CancellationToken.None);

        Assert.Equal("KRM", result.Code);
        Assert.Equal(42, result.SubiektContractorId);
        Assert.NotNull(store.AddedCustomer);
        Assert.Equal("CustomerCreated", store.Audit!.Action);
    }

    [Fact]
    public async Task Creating_customer_rejects_missing_subiekt_reference()
    {
        var actor = new CurrentActor(ActorKind.Administrator, Guid.NewGuid(), null, "Administrator",
            Permissions.For(ActorKind.Administrator), Guid.NewGuid());
        var handler = new CreateCustomerHandler(new CustomerStoreStub(), new ContractorDirectoryStub(false),
            new ApplicationAuthorizationService(new ActorAccessorStub(actor)), new AuditEntryFactory(), TimeProvider.System);

        await Assert.ThrowsAsync<RequestValidationException>(() => handler.Handle(
            new CreateCustomerCommand("KRM", "Kramp", null, 42, null, true), CancellationToken.None));
    }

    private sealed class CustomerStoreStub : ICustomerStore
    {
        public Customer? AddedCustomer { get; private set; }
        public AuditEntry? Audit { get; private set; }

        public Task<CustomerStoreResult> AddAsync(Customer customer, AuditEntry audit, CancellationToken cancellationToken)
        {
            AddedCustomer = customer;
            Audit = audit;
            return Task.FromResult(CustomerStoreResult.Success);
        }

        public Task<PagedResult<CustomerListItemDto>> ListAsync(string? search, bool? isActive, int page, int pageSize,
            CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Customer?> FindAsync(Guid id, bool tracking, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<CustomerSite?> FindSiteAsync(Guid customerId, Guid siteId, bool tracking, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<PagedResult<CustomerSiteListItemDto>> ListSitesAsync(Guid customerId, string? search, int page,
            int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<PagedResult<CustomerActivityDto>> ListActivityAsync(Guid customerId, int page, int pageSize,
            CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CustomerStoreResult> SaveCustomerAsync(Customer customer, long expectedVersion, AuditEntry audit,
            CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CustomerStoreResult> SaveSiteAsync(CustomerSite site, long expectedVersion, AuditEntry audit,
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class ContractorDirectoryStub(bool exists) : ICustomerContractorDirectory
    {
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) => Task.FromResult(exists);
        public Task<PagedResult<CustomerContractorDto>> SearchAsync(string? search, int page, int pageSize,
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class ActorAccessorStub(CurrentActor actor) : ICurrentActorAccessor
    {
        public CurrentActor? Actor { get; } = actor;
    }
}
