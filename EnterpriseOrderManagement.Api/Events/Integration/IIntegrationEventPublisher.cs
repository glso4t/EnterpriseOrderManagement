using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Events.Integration;

// Abstraction over the transport that delivers Outbox messages
// (a logging stub today, RabbitMQ later).
public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);
}