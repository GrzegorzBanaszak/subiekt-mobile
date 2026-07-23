using SubiektMobile.Domain.CustomerOrders;
using Xunit;

namespace SubiektMobile.Domain.Tests;

public sealed class CustomerOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Draft_order_allows_separate_source_lines_for_the_same_part()
    {
        var order = Create();

        order.AddItem(Guid.NewGuid(), "P-100", 2m, Guid.NewGuid(), "Administrator", Now.AddMinutes(1));
        order.AddItem(Guid.NewGuid(), "P-100", 3.5m, Guid.NewGuid(), "Administrator", Now.AddMinutes(2));

        Assert.Equal(2, order.Items.Count);
        Assert.Equal(3, order.Version);
    }

    [Fact]
    public void Ready_order_cannot_be_edited_and_can_be_converted_once()
    {
        var actorId = Guid.NewGuid();
        var order = Create(actorId);
        order.AddItem(Guid.NewGuid(), "P-100", 2m, actorId, "Administrator", Now.AddMinutes(1));

        order.MarkReady(actorId, "Administrator", Now.AddMinutes(2));
        Assert.Throws<InvalidOperationException>(() =>
            order.Update(null, null, new DateOnly(2026, 8, 1), null, actorId, "Administrator", Now.AddMinutes(3)));

        order.MarkConverted(actorId, "Administrator", Now.AddMinutes(4));

        Assert.Equal(CustomerOrderStatus.Converted, order.Status);
        Assert.Throws<InvalidOperationException>(() => order.MarkConverted(actorId, "Administrator", Now.AddMinutes(5)));
        Assert.Throws<InvalidOperationException>(() => order.Cancel(actorId, "Administrator", Now.AddMinutes(5)));
    }

    [Fact]
    public void Optional_references_are_normalized_to_null_and_quantities_are_limited_to_four_decimals()
    {
        var order = Create();
        Assert.Null(order.CustomerOrderNumber);
        Assert.Null(order.DeliveryNoteNumber);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            order.AddItem(Guid.NewGuid(), "P-100", 1.00001m, Guid.NewGuid(), "Administrator", Now));
    }

    private static CustomerOrder Create(Guid? actorId = null) => CustomerOrder.Create(Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), " ", " ", new DateOnly(2026, 8, 1), null, actorId ?? Guid.NewGuid(), "Administrator", Now);
}
