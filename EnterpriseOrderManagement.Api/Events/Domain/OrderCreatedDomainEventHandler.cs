using System.Text.Json;

using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Events.Domain;

public sealed class OrderCreatedDomainEventHandler(
    EnterpriseOrderManagementDbContext dbContext)
    : IDomainEventHandler<OrderCreatedDomainEvent>
{
    private const string MessageType =
        "orders.order-created.v1";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task HandleAsync(
        OrderCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new OrderCreatedIntegrationEvent(
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
        dbContext.OutboxMessages.Add(outboxMessage);
        return Task.CompletedTask;
    }
}