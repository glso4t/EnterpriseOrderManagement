
using System.Text;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.Events.Integration;


// Publishes Outbox messages through one reusable RabbitMQ publisher channel.
// The channel is created lazily so application startup does not depend on
// RabbitMQ being available.
public sealed class RabbitMqIntegrationEventPublisher(
    IRabbitMqConnectionManager connectionManager,
    ILogger<RabbitMqIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher, IAsyncDisposable
{
    private const string ExchangeName = "enterprise.events";

    // A single message must be confirmed within this time. The timeout also
    // includes waiting for exclusive access to the shared publisher channel.
    private static readonly TimeSpan PublishTimeout =
        TimeSpan.FromSeconds(5);
    private readonly SemaphoreSlim _channelSemaphore = new(1, 1);

    private IChannel? _channel;
    private bool _disposed;



    public async Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.Type))
        {
            throw new ArgumentException(
                "Outbox message Type cannot be empty.",
                nameof(message));
        }

        // The Outbox event Type is also our routing key.
        string routingKey = message.Type;

        // The timeout covers connection, channel creation and publishing.
        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
        timeoutCts.CancelAfter(PublishTimeout);

        try
        {
            // RabbitMQ.Client 7.2.2 does not support concurrent publishing
            // through the same IChannel. Keep exclusive access until the
            // publish operation (including its publisher confirmation) ends.
            await _channelSemaphore.WaitAsync(timeoutCts.Token);

            try
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(
                        nameof(RabbitMqIntegrationEventPublisher));
                }

                var channel = await GetOrCreateChannelAsync(
                    timeoutCts.Token);

                byte[] body = Encoding.UTF8.GetBytes(message.Content);

                var properties = new BasicProperties
                {
                    MessageId = message.Id.ToString(),
                    Type = message.Type,
                    ContentType = "application/json",
                    Persistent = true,
                    Timestamp = new AmqpTimestamp(
                        message.OccurredOnUtc.ToUnixTimeSeconds())
                };

                // With confirmations enabled, this await completes only
                // after RabbitMQ confirms the publication. mandatory=true
                // also makes an unroutable message fail.
                await channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: timeoutCts.Token);
            }
            finally
            {
                _channelSemaphore.Release();
            }

            logger.LogInformation(
                "Published Outbox message {MessageId} with routing key {RoutingKey}.",
                message.Id,
                routingKey);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested &&
                timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"RabbitMQ did not confirm Outbox message {message.Id} " +
                $"within {PublishTimeout}.");
        }
    }

    /// Returns the shared channel or creates it when this publisher is used
    /// for the first time.
    /// The caller must hold _channelSemaphore.
        private async Task<IChannel> GetOrCreateChannelAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true } openChannel)
        {
            return openChannel;
        }

        // Do not silently replace a channel that has already been created.
        // A closed channel may be temporarily unavailable while the
        // connection performs automatic recovery. The dedicated recovery
        // logic (including permanently closed channels) is covered in 12.7.
        if (_channel is not null)
        {
            throw new InvalidOperationException(
                "The RabbitMQ publisher channel is closed and cannot be used.");
        }

        var connection = await connectionManager.GetConnectionAsync(
            cancellationToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        var channel = await connection.CreateChannelAsync(
            channelOptions,
            cancellationToken);

        _channel = channel;
        return channel;
    }

    // Closes and disposes the reusable channel during application shutdown.
    // The same semaphore prevents disposal from racing with an active publish.
    public async ValueTask DisposeAsync()
    {
        await _channelSemaphore.WaitAsync();

        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var channel = _channel;
            _channel = null;

            if (channel is null)
            {
                return;
            }

            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync(
                        replyCode: 200,
                        replyText: "Publisher is shutting down.",
                        abort: false);
                }
            }
            catch (Exception exception)
            {
                // Graceful close is best-effort: the broker may already be
                // unavailable during shutdown. Dispose must still run.
                logger.LogWarning(
                    exception,
                    "Could not gracefully close the RabbitMQ publisher channel.");
            }
            finally
            {
                await channel.DisposeAsync();
            }
        }
        finally
        {
            _channelSemaphore.Release();
        }

        // The semaphore is intentionally left for garbage collection. A
        // publish may already be waiting on it during shutdown; disposing the
        // semaphore here would race with those waiters.
    }
}
