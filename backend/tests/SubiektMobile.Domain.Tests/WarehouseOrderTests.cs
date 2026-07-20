using SubiektMobile.Domain.WarehouseOrders;
using SubiektMobile.Domain.Identity;
using System.Globalization;
using Xunit;

namespace SubiektMobile.Domain.Tests;

public sealed class WarehouseOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ActorId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Order_keeps_product_snapshot_and_increments_version()
    {
        var order = Create();
        order.AddItem(7, "Original name", "ABC", 2.5m, "szt.", 1.2m, ActorId, "Admin", Now);

        var item = Assert.Single(order.Items);
        Assert.Equal("Original name", item.ProductName);
        Assert.Equal(2.5m, item.Quantity);
        Assert.Equal(1.2m, item.UnitWeightKg);
        Assert.Equal(2, order.Version);
    }

    [Fact]
    public void Duplicate_product_is_rejected()
    {
        var order = Create();
        order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);

        Assert.Throws<InvalidOperationException>(() =>
            order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now));
    }

    [Fact]
    public void Empty_order_cannot_be_published()
    {
        var order = Create();
        Assert.Throws<InvalidOperationException>(() => order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now));
    }

    [Fact]
    public void Single_assignee_order_requires_exactly_one_employee_when_published()
    {
        var order = WarehouseOrder.Create(Guid.NewGuid(), "ZAM-2", "Customer",
            new DateOnly(2026, 7, 6), ActorId, "Admin", Now);
        order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);

        Assert.Throws<InvalidOperationException>(() =>
            order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now));
    }

    [Fact]
    public void Shared_order_accepts_multiple_distinct_employees()
    {
        var order = WarehouseOrder.Create(Guid.NewGuid(), "ZAM-3", "Customer",
            new DateOnly(2026, 7, 6), ActorId, "Admin", Now, PickingMode.SharedTeam,
            [Candidate("22222222-2222-2222-2222-222222222222", "Employee One"),
             Candidate("44444444-4444-4444-4444-444444444444", "Employee Two")]);
        order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);

        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        Assert.Equal(WarehouseOrderStatus.ReadyForPicking, order.Status);
        Assert.Equal(2, order.Assignees.Count);
    }

    [Fact]
    public void Published_order_cannot_be_modified()
    {
        var order = Create();
        order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        Assert.Equal(WarehouseOrderStatus.ReadyForPicking, order.Status);
        Assert.Throws<InvalidOperationException>(() =>
            order.UpdateHeader("Other", new DateOnly(2026, 7, 7), ActorId, "Admin", Now));
    }

    [Fact]
    public void Published_order_cannot_be_deleted()
    {
        var order = Create();
        order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        Assert.Throws<InvalidOperationException>(order.EnsureCanDelete);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_quantity_is_rejected(decimal quantity)
    {
        var order = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            order.AddItem(7, "Name", null, quantity, "szt.", null, ActorId, "Admin", Now));
    }

    [Fact]
    public void Shared_item_requires_reservation_and_can_be_partially_packed()
    {
        var employee = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var order = Create(PickingMode.SharedTeam);
        var item = order.AddItem(7, "Name", null, 10, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        Assert.Throws<InvalidOperationException>(() => order.PackItem(item.Id, 4, employee, false, Now));
        order.ReserveItem(item.Id, employee, Now);
        order.PackItem(item.Id, 4, employee, false, Now.AddMinutes(1));

        Assert.Equal(WarehouseOrderItemStatus.Picking, item.Status);
        Assert.Equal(4, item.PackedQuantity);
        Assert.Equal(3, item.Version);
        Assert.Equal(employee.Id, item.ReservedById);
    }

    [Fact]
    public void Single_assignee_can_pack_remaining_quantity_in_multiple_batches()
    {
        var employee = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var order = Create();
        var item = order.AddItem(7, "Name", null, 10, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        order.PackItem(item.Id, 4, employee, false, Now);

        Assert.Equal(WarehouseOrderItemStatus.ToPick, item.Status);
        Assert.Equal(4, item.PackedQuantity);

        order.PackItem(item.Id, 6, employee, false, Now.AddMinutes(1));

        Assert.Equal(WarehouseOrderItemStatus.Packed, item.Status);
        Assert.Equal(10, item.PackedQuantity);
    }

    [Theory]
    [InlineData("10.0001")]
    [InlineData("0")]
    [InlineData("-0.0001")]
    [InlineData("1.00001")]
    public void Invalid_packed_quantity_is_rejected(string value)
    {
        var employee = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var order = Create();
        var item = order.AddItem(7, "Name", null, 10, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            order.PackItem(item.Id, decimal.Parse(value, CultureInfo.InvariantCulture), employee, false, Now));
    }

    [Fact]
    public void Released_partial_shared_item_can_be_completed_by_another_employee()
    {
        var first = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee one");
        var second = new PickingActor(ActorKind.Employee, Guid.NewGuid(), "Employee two");
        var order = Create(PickingMode.SharedTeam);
        var item = order.AddItem(7, "Name", null, 10, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        order.ReserveItem(item.Id, first, Now);
        order.PackItem(item.Id, 4, first, false, Now.AddMinutes(1));
        order.ReleaseItem(item.Id, first, false, Now.AddMinutes(2));
        order.ReserveItem(item.Id, second, Now.AddMinutes(3));
        order.PackItem(item.Id, 6, second, false, Now.AddMinutes(4));

        Assert.Equal(WarehouseOrderItemStatus.Packed, item.Status);
        Assert.Equal(10, item.PackedQuantity);
        Assert.Null(item.ReservedById);
        Assert.Equal(second.Id, item.PackedById);
    }

    [Fact]
    public void Only_reservation_owner_can_release_but_administrator_can_override()
    {
        var owner = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var other = new PickingActor(ActorKind.Employee, Guid.NewGuid(), "Other employee");
        var admin = new PickingActor(ActorKind.Administrator, ActorId, "Admin");
        var order = Create(PickingMode.SharedTeam);
        var item = order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);
        order.ReserveItem(item.Id, owner, Now);

        Assert.Throws<InvalidOperationException>(() => order.ReleaseItem(item.Id, other, false, Now));
        order.ReleaseItem(item.Id, admin, true, Now);

        Assert.Equal(WarehouseOrderItemStatus.ToPick, item.Status);
    }

    [Fact]
    public void Undo_packing_restores_available_item_and_clears_packed_quantity()
    {
        var employee = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var order = Create();
        var item = order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);
        order.PackItem(item.Id, 1, employee, false, Now);

        order.UndoPackedItem(item.Id, employee, false, Now.AddMinutes(1));

        Assert.Equal(WarehouseOrderItemStatus.ToPick, item.Status);
        Assert.Null(item.PackedQuantity);
        Assert.Null(item.PackedById);
    }

    [Fact]
    public void Pallet_assignment_marks_item_assigned_only_after_full_quantity_is_palletized()
    {
        var employee = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var order = Create();
        var item = order.AddItem(7, "Name", null, 20, "szt.", 1.5m, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        order.PackItem(item.Id, 10, employee, false, Now);
        order.AssignPackedQuantityToPallet(item.Id, 10);

        Assert.Equal(WarehouseOrderItemStatus.ToPick, item.Status);
        Assert.Equal(10, item.PackedQuantity);

        order.PackItem(item.Id, 10, employee, false, Now.AddMinutes(1));
        order.AssignPackedQuantityToPallet(item.Id, 20);

        Assert.Equal(WarehouseOrderItemStatus.AssignedToPallet, item.Status);
    }

    [Fact]
    public void Pallet_assignment_cannot_exceed_packed_quantity()
    {
        var employee = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var order = Create();
        var item = order.AddItem(7, "Name", null, 20, "szt.", 1.5m, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);
        order.PackItem(item.Id, 10, employee, false, Now);

        Assert.Throws<InvalidOperationException>(() =>
            order.AssignPackedQuantityToPallet(item.Id, 11));
    }

    [Fact]
    public void Undo_packing_is_blocked_after_pallet_assignment()
    {
        var employee = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var order = Create();
        var item = order.AddItem(7, "Name", null, 20, "szt.", 1.5m, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);
        order.PackItem(item.Id, 10, employee, false, Now);

        Assert.Throws<InvalidOperationException>(() =>
            order.UndoPackedItem(item.Id, employee, false, Now.AddMinutes(1), 10));
    }

    [Fact]
    public void Closed_pallet_requires_items_and_valid_weights()
    {
        var actor = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() => Pallet.CreateClosed(Guid.NewGuid(),
            Guid.NewGuid(), orderId, "PAL-1", 0, [], actor, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Pallet.CreateClosed(Guid.NewGuid(),
            Guid.NewGuid(), orderId, "PAL-1", -0.0001m,
            [new PalletItemAllocation(itemId, 1, 1)], actor, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Pallet.CreateClosed(Guid.NewGuid(),
            Guid.NewGuid(), orderId, "PAL-1", 0,
            [new PalletItemAllocation(itemId, 1, 0)], actor, Now));
    }

    [Fact]
    public void Closed_pallet_calculates_goods_and_total_weight()
    {
        var actor = new PickingActor(ActorKind.Employee,
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "Employee");

        var pallet = Pallet.CreateClosed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "PAL-1", 25m,
            [
                new PalletItemAllocation(Guid.NewGuid(), 10, 1.5m),
                new PalletItemAllocation(Guid.NewGuid(), 2, 4.25m)
            ], actor, Now);

        Assert.Equal(23.5m, pallet.GoodsWeightKg);
        Assert.Equal(48.5m, pallet.TotalWeightKg);
    }

    private static WarehouseOrder Create() => WarehouseOrder.Create(Guid.NewGuid(), "ZAM-1", "Customer",
        new DateOnly(2026, 7, 6), ActorId, "Admin", Now, PickingMode.SingleAssignee,
        [Candidate("22222222-2222-2222-2222-222222222222", "Employee")]);

    private static WarehouseOrder Create(PickingMode mode) => WarehouseOrder.Create(Guid.NewGuid(), "ZAM-1", "Customer",
        new DateOnly(2026, 7, 6), ActorId, "Admin", Now, mode,
        [Candidate("22222222-2222-2222-2222-222222222222", "Employee")]);

    private static WarehouseOrderAssigneeCandidate Candidate(string employeeId, string name) =>
        new(Guid.Parse(employeeId), Guid.Parse("33333333-3333-3333-3333-333333333333"), name);
}
