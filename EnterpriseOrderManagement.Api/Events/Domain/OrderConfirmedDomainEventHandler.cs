using System.Text.Json;

using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Events.Domain;

public sealed class OrderConfirmedDomainEventHandler(
    EnterpriseOrderManagementDbContext dbContext)
    : IDomainEventHandler<OrderConfirmedDomainEvent>
{
    private const string MessageType =
        "orders.order-confirmed.v1";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task HandleAsync(
        OrderConfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new OrderConfirmedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OrderId: domainEvent.OrderId,
            CustomerId: domainEvent.CustomerId,
            TotalAmount: domainEvent.TotalAmount,
            OccurredOnUtc: domainEvent.OccurredOnUtc,
            SchemaVersion: 1
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

        return Task.CompletedTask;
    }
}