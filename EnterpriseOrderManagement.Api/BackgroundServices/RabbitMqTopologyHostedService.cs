
using EnterpriseOrderManagement.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

// Declares the RabbitMQ exchanges, queues and bindings.
// The API can start even when RabbitMQ is unavailable: the Outbox
// keeps events until the broker becomes available again.
public sealed class RabbitMqTopologyHostedService(
    RabbitMqConnectionManager connectionManager,
    ILogger<RabbitMqTopologyHostedService> logger)
    : BackgroundService
{
    private const string MainExchangeName = "enterprise.events";
    private const string MainQueueName = "notification-service";
    private const string MainRoutingKeyPattern = "orders.#";
    private const string DeadLetterExchangeName =
        "enterprise.events.dlx";
    private const string DeadLetterQueueName =
        "notification-service.dlq";
    private const string DeadLetterRoutingKey =
        "notification-service.dead";
    private static readonly TimeSpan RetryDelay =
        TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeclareTopologyAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    "RabbitMQ topology declaration failed: {Reason} " +
                    "Retrying in {RetryDelay}.",
                    exception.Message,
                    RetryDelay);

                try
                {
                    await Task.Delay(RetryDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task DeclareTopologyAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Initializing RabbitMQ topology...");

        var connection = await connectionManager.GetConnectionAsync(
            cancellationToken);

        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        // 1. Main exchange: normal integration events enter here.
        await channel.ExchangeDeclareAsync(
            exchange: MainExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // 2. Dead Letter Exchange: dead-lettered messages are routed here.
        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // 3. Dead Letter Queue: stores messages that could not be processed.
        await channel.QueueDeclareAsync(
            queue: DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: DeadLetterQueueName,
            exchange: DeadLetterExchangeName,
            routingKey: DeadLetterRoutingKey,
            cancellationToken: cancellationToken);

        // 4. Main queue remains declared without x-arguments.
        // Its DLX behavior is configured through a RabbitMQ policy.
        // Policies can be changed without deleting/recreating this queue.
        await channel.QueueDeclareAsync(
            queue: MainQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: MainQueueName,
            exchange: MainExchangeName,
            routingKey: MainRoutingKeyPattern,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "RabbitMQ topology initialized. " +
            "Main exchange: {MainExchange}, Main queue: {MainQueue}, " +
            "DLX: {DeadLetterExchange}, DLQ: {DeadLetterQueue}.",
            MainExchangeName,
            MainQueueName,
            DeadLetterExchangeName,
            DeadLetterQueueName);
    }
}
