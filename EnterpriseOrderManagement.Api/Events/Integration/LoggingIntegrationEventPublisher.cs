using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Events.Integration;

// Temporary stub: only logs. It will be replaced by a RabbitMQ publisher.
public sealed class LoggingIntegrationEventPublisher(
    ILogger<LoggingIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    public Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Simulated publish of Outbox message {MessageId} of type {MessageType}.",
            message.Id,
            message.Type);
        return Task.CompletedTask;
    }
}