using EnterpriseOrderManagement.Api.Events.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Tests;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_InvokesRegisteredHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        var handler = new TestOrderCreatedHandler();

        services.AddSingleton<
            IDomainEventHandler<OrderCreatedDomainEvent>>(
                handler);

        using var provider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(provider);

        var domainEvent = new OrderCreatedDomainEvent(
            OrderId: 1,
            CustomerId: 10,
            TotalAmount: 150.00m,
            OccurredOnUtc: DateTimeOffset.UtcNow
        );

        // Act
        await dispatcher.DispatchAsync(domainEvent);

        // Assert
        Assert.NotNull(handler.ReceivedEvent);

        Assert.Equal(
            domainEvent.OrderId,
            handler.ReceivedEvent.OrderId);
    }


    private sealed class TestOrderCreatedHandler
        : IDomainEventHandler<OrderCreatedDomainEvent>
    {
        public OrderCreatedDomainEvent? ReceivedEvent { get; private set; }

        public Task HandleAsync(
            OrderCreatedDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            ReceivedEvent = domainEvent;

            return Task.CompletedTask;
        }
    }
}