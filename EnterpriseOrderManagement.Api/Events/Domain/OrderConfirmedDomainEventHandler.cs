using System.Text.Json;

using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Events.Domain;

public sealed class OrderConfirmedDomainEventHandler(
    EnterpriseOrderManagementDbContext dbContext,
    ICustomerSnapshotProvider customerSnapshotProvider)
    : IDomainEventHandler<OrderConfirmedDomainEvent>
{
    private const string MessageType =
        "orders.order-confirmed.v2";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task HandleAsync(
        OrderConfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var customerSnapshot = await customerSnapshotProvider.GetAsync(
            domainEvent.CustomerId,
            cancellationToken);
        var integrationEvent = new OrderConfirmedIntegrationEvent(
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

        // Only adds the message to the DbContext.
        // The caller's transaction saves it together with the business change.
        dbContext.OutboxMessages.Add(outboxMessage);
    }
}