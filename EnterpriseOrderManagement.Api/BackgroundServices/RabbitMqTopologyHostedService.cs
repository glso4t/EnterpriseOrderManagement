
using EnterpriseOrderManagement.Api.Services.Interfaces;
using EnterpriseOrderManagement.Api.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

// Declares the RabbitMQ exchanges, queues and bindings.
// The API can start even when RabbitMQ is unavailable: the Outbox
// keeps events until the broker becomes available again.
public sealed class RabbitMqTopologyHostedService(
    IRabbitMqConnectionManager connectionManager,
    ILogger<RabbitMqTopologyHostedService> logger)
    : BackgroundService
{
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
            exchange: RabbitMqTopology.MainExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // 2. Dead Letter Exchange.
        await channel.ExchangeDeclareAsync(
            exchange: RabbitMqTopology.DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // 3. Dead Letter Queue.
        await channel.QueueDeclareAsync(
            queue: RabbitMqTopology.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: RabbitMqTopology.DeadLetterQueueName,
            exchange: RabbitMqTopology.DeadLetterExchangeName,
            routingKey: RabbitMqTopology.DeadLetterRoutingKey,
            cancellationToken: cancellationToken);

        // 4. Main queue remains declared without x-arguments.
        // Apply infrastructure/rabbitmq/configure-dlx.ps1 before
        // using this queue.
        // The version-controlled policy supplies the DLX and routing key.
        // Policies can be changed without deleting/recreating this queue.
        await channel.QueueDeclareAsync(
            queue: RabbitMqTopology.MainQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: RabbitMqTopology.MainQueueName,
            exchange: RabbitMqTopology.MainExchangeName,
            routingKey: RabbitMqTopology.MainRoutingKeyPattern,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "RabbitMQ topology initialized. " +
            "Main exchange: {MainExchange}, Main queue: {MainQueue}, " +
            "DLX: {DeadLetterExchange}, DLQ: {DeadLetterQueue}.",
            RabbitMqTopology.MainExchangeName,
            RabbitMqTopology.MainQueueName,
            RabbitMqTopology.DeadLetterExchangeName,
            RabbitMqTopology.DeadLetterQueueName);
    }
}
