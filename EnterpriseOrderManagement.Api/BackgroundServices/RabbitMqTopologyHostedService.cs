
using EnterpriseOrderManagement.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.BackgroundServices;


// Declares the RabbitMQ topology in the background.
// The API must start, and keep accepting requests, even when the broker
// is down: the Outbox keeps the events until the broker is back.
public sealed class RabbitMqTopologyHostedService(
    RabbitMqConnectionManager connectionManager,
    ILogger<RabbitMqTopologyHostedService> logger)
    : BackgroundService
{
    private const string ExchangeName = "enterprise.events";
    private const string QueueName = "notification-service";
    private const string RoutingKeyPattern = "order.#";
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
                    "RabbitMQ topology declaration failed: {Reason} Retrying in {RetryDelay}.",
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

        // Topic exchange: routes messages by routing key pattern.
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Durable queue: survives a broker restart.
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Binds the queue to the exchange with the routing key pattern.
        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKeyPattern,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "RabbitMQ topology initialized successfully. " +
            "Exchange: {Exchange}, Queue: {Queue}, Binding: {Binding}",
            ExchangeName,
            QueueName,
            RoutingKeyPattern);
    }
}
