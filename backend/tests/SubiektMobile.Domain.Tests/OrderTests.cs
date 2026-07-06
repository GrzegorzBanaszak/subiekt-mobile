using SubiektMobile.Domain.Orders;
using Xunit;

namespace SubiektMobile.Domain.Tests;

public sealed class OrderTests
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
        var order = Order.Create(Guid.NewGuid(), "ZAM-2", "Customer",
            new DateOnly(2026, 7, 6), ActorId, "Admin", Now);
        order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);

        Assert.Throws<InvalidOperationException>(() =>
            order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now));
    }

    [Fact]
    public void Shared_order_accepts_multiple_distinct_employees()
    {
        var order = Order.Create(Guid.NewGuid(), "ZAM-3", "Customer",
            new DateOnly(2026, 7, 6), ActorId, "Admin", Now, PickingMode.SharedTeam,
            [Candidate("22222222-2222-2222-2222-222222222222", "Employee One"),
             Candidate("44444444-4444-4444-4444-444444444444", "Employee Two")]);
        order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);

        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        Assert.Equal(OrderStatus.ReadyForPicking, order.Status);
        Assert.Equal(2, order.Assignees.Count);
    }

    [Fact]
    public void Published_order_cannot_be_modified()
    {
        var order = Create();
        order.AddItem(7, "Name", null, 1, "szt.", null, ActorId, "Admin", Now);
        order.Publish(new DateOnly(2026, 7, 5), ActorId, "Admin", Now);

        Assert.Equal(OrderStatus.ReadyForPicking, order.Status);
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

    private static Order Create() => Order.Create(Guid.NewGuid(), "ZAM-1", "Customer",
        new DateOnly(2026, 7, 6), ActorId, "Admin", Now, PickingMode.SingleAssignee,
        [Candidate("22222222-2222-2222-2222-222222222222", "Employee")]);

    private static OrderAssigneeCandidate Candidate(string employeeId, string name) =>
        new(Guid.Parse(employeeId), Guid.Parse("33333333-3333-3333-3333-333333333333"), name);
}
