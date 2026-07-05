using Microsoft.EntityFrameworkCore;
using SubiektMobile.Application.Orders;
using SubiektMobile.Domain.Identity;
using SubiektMobile.Domain.Orders;
using SubiektMobile.Infrastructure.Orders;
using SubiektMobile.Infrastructure.Persistence.Application;
using Xunit;

namespace SubiektMobile.Infrastructure.IntegrationTests;

[Collection(PostgreSqlIdentityCollection.Name)]
public sealed class OrderStoreTests
{
    [PostgreSqlFact]
    public async Task First_item_is_saved_with_expected_order_version()
    {
        var connectionString = Environment.GetEnvironmentVariable("SUBIEKT_MOBILE_TEST_DB")!;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var store = new OrderStore(context);
        var now = new DateTimeOffset(2026, 7, 5, 20, 0, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), $"TEST-{Guid.NewGuid():N}", "Test customer",
            new DateOnly(2026, 7, 6), actorId, "Test actor", now);

        Assert.Equal(OrderStoreResult.Success,
            await store.AddAsync(order, Audit(actorId, order.Id, "OrderCreated", now), CancellationToken.None));
        order.AddItem(1, "Test product", "TEST", 1, "szt.", 0.2m,
            actorId, "Test actor", now.AddMinutes(1));

        var result = await store.SaveAsync(order, 1,
            Audit(actorId, order.Id, "OrderItemAdded", now.AddMinutes(1)), CancellationToken.None);

        Assert.Equal(OrderStoreResult.Success, result);
        Assert.Equal(2, order.Version);
        Assert.Single(await context.OrderItems.Where(x => x.OrderId == order.Id).ToListAsync());
        await transaction.RollbackAsync();
    }

    private static AuditEntry Audit(Guid actorId, Guid orderId, string action, DateTimeOffset now) =>
        AuditEntry.Create(ActorKind.Administrator, actorId, null, "Test actor", action, "Order", orderId, now);
}
