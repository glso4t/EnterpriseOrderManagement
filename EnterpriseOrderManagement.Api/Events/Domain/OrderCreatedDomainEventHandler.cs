using System.Text.Json;

using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Events.Domain;

public sealed class OrderCreatedDomainEventHandler(
    EnterpriseOrderManagementDbContext dbContext,
    ICustomerSnapshotProvider customerSnapshotProvider)
    : IDomainEventHandler<OrderCreatedDomainEvent>
{
    private const string MessageType =
        "orders.order-created.v2";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task HandleAsync(
        OrderCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        // Snapshot of the customer data at the time of the event.
        var customerSnapshot = await customerSnapshotProvider.GetAsync(
            domainEvent.CustomerId,
            cancellationToken);

        var integrationEvent = new OrderCreatedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OrderId: domainEvent.OrderId,
            CustomerId: domainEvent.CustomerId,
            Customer: customerSnapshot,
            TotalAmount: domainEvent.TotalAmount,
            OccurredOnUtc: domainEvent.OccurredOnUtc,
            SchemaVersion: 2
        );

        var outboxMessage = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            Type = MessageType,
            Content = JsonSerializer.Serialize(
                integrationEvent,
                JsonOptions),
            OccurredOnUtc = integrationEvent.OccurredOnUtc
        };

        dbContext.OutboxMessages.Add(outboxMessage);
    }
}