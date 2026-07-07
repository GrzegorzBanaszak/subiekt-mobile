using SubiektMobile.Application.Identity;
using SubiektMobile.Application.Picking;
using SubiektMobile.Application.Products;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;
using Xunit;

namespace SubiektMobile.Application.Tests;

public sealed class PickingUseCasesTests
{
    [Fact]
    public async Task Duplicate_pack_operation_with_different_quantity_is_rejected()
    {
        var now = new DateTimeOffset(2026, 7, 5, 10, 0, 0, TimeSpan.Zero);
        var organizationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var employeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var creatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var actor = new CurrentActor(
            ActorKind.Employee,
            employeeId,
            organizationId,
            "Picker",
            Permissions.For(ActorKind.Employee),
            Guid.NewGuid());
        var pickingActor = new PickingActor(ActorKind.Employee, employeeId, "Picker");
        var operationId = Guid.NewGuid();

        var order = Order.Create(
            Guid.NewGuid(),
            "ZAM-1",
            "Customer",
            new DateOnly(2026, 7, 6),
            creatorId,
            "Creator",
            now,
            PickingMode.SingleAssignee,
            [new OrderAssigneeCandidate(employeeId, organizationId, "Picker")]);
        var item = order.AddItem(7, "Test product", "TP", 10m, "szt.", 1.2m, creatorId, "Creator", now);
        order.Publish(new DateOnly(2026, 7, 5), creatorId, "Creator", now);
        order.PackItem(item.Id, 3m, pickingActor, false, now.AddMinutes(1));

        var previousOperation = OrderPickingEvent.Create(
            operationId,
            order,
            item,
            PickingAction.Packed,
            OrderItemStatus.ToPick,
            3m,
            pickingActor,
            now.AddMinutes(1));

        var store = new PickingStoreStub(order, previousOperation);
        var handler = new PackPickingItemHandler(
            store,
            new ApplicationAuthorizationService(new CurrentActorAccessorStub(actor)),
            new AuditEntryFactory(),
            TimeProvider.System);

        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            handler.Handle(
                new PackPickingItemCommand(order.Id, item.Id, operationId, item.Version, 4m),
                CancellationToken.None));
    }

    private sealed class PickingStoreStub(Order order, OrderPickingEvent operation) : IPickingStore
    {
        public Task<PagedResult<PickingOrderListItemDto>> ListAsync(PickingOrderListFilter filter,
            CurrentActor actor, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Order?> FindOrderAsync(Guid orderId, bool tracking, CancellationToken cancellationToken) =>
            Task.FromResult<Order?>(orderId == order.Id ? order : null);

        public Task<PagedResult<PickingHistoryItemDto>> ListHistoryAsync(Guid orderId, int page, int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<OrderPickingEvent?> FindOperationAsync(Guid operationId, CancellationToken cancellationToken) =>
            Task.FromResult<OrderPickingEvent?>(operationId == operation.OperationId ? operation : null);

        public Task<PickingStoreMutationResult> SaveMutationAsync(OrderItem item, long expectedVersion,
            OrderPickingEvent pickingEvent, AuditEntry audit, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class CurrentActorAccessorStub(CurrentActor? actor) : ICurrentActorAccessor
    {
        public CurrentActor? Actor { get; } = actor;
    }
}
