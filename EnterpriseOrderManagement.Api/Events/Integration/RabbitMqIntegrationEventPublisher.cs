
using System.Text;
using System.Text.Json;
using EnterpriseOrderManagement.Api.Configuration;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.Events.Integration;

/// <summary>
/// Publishes Outbox messages through a reusable RabbitMQ channel.
/// Closed channels are retired and replaced when the connection
/// is available.
/// </summary>
public sealed class RabbitMqIntegrationEventPublisher(
    IRabbitMqConnectionManager connectionManager,
    ILogger<RabbitMqIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher, IAsyncDisposable
{
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
        ValidateMessage(message);
        string routingKey = message.Type;

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutCts.CancelAfter(PublishTimeout);

        try
        {
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

                // Perform one publish attempt.
                // The Outbox is responsible for scheduling retries.
                await channel.BasicPublishAsync(
                    exchange: RabbitMqTopology.MainExchangeName,
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
                $"RabbitMQ publish operation for Outbox message " +
                $"{message.Id} exceeded {PublishTimeout}.");
        }
    }

///Methods
    private static void ValidateMessage(OutboxMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Type))
        {
            throw new InvalidIntegrationMessageException(
                "Outbox message Type cannot be empty.");
        }

        if (Encoding.UTF8.GetByteCount(message.Type) > 255)
        {
            throw new InvalidIntegrationMessageException(
                "Outbox message routing key exceeds 255 UTF-8 bytes.");
        }

        if (string.IsNullOrWhiteSpace(message.Content))
        {
            throw new InvalidIntegrationMessageException(
                "Outbox message content cannot be empty.");
        }

        try
        {
            using var document =
                JsonDocument.Parse(message.Content);
        }
        catch (JsonException exception)
        {
            throw new InvalidIntegrationMessageException(
                "Outbox message content is not valid JSON.",
                exception);
        }
    }
    /// <summary>
    /// Returns an existing usable channel or creates a replacement.
    /// The caller must hold _channelSemaphore.
    /// </summary>
    private async Task<IChannel> GetOrCreateChannelAsync(
        CancellationToken cancellationToken)
    {
        // Check connection availability before touching the channel.
        // The connection manager retains ownership during recovery.
        var connection = await connectionManager.GetConnectionAsync(
            cancellationToken);

        if (_channel is { IsOpen: true } openChannel)
        {
            return openChannel;
        }

        if (_channel is not null)
        {
            logger.LogWarning(
                "RabbitMQ publisher channel is closed. " +
                "Retiring the old channel before creating a replacement.");

            await RetireChannelAsync(cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        _channel = await connection.CreateChannelAsync(
            channelOptions,
            cancellationToken);

        logger.LogInformation(
            "RabbitMQ publisher channel created with confirmations enabled.");

        return _channel;
    }

    /// <summary>
    /// Retires a closed or unavailable publisher channel.
    /// Explicit close removes the channel from automatic recovery
    /// tracking. Disposal is attempted even if close fails.
    /// The caller must hold _channelSemaphore.
    /// </summary>
    private async Task RetireChannelAsync(
        CancellationToken cancellationToken)
    {
        var oldChannel = _channel;

        if (oldChannel is null)
        {
            return;
        }

        _channel = null;

        try
        {
            await oldChannel.CloseAsync(
                replyCode: 200,
                replyText: "Replacing unavailable publisher channel.",
                abort: true,
                cancellationToken: cancellationToken);
        }
        finally
        {
            await oldChannel.DisposeAsync();
        }
    }

    /// <summary>
    /// Closes and disposes the publisher channel during shutdown.
    /// </summary>
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
                else
                {
                    await channel.CloseAsync(
                        replyCode: 200,
                        replyText: "Publisher is shutting down.",
                        abort: true);
                }
            }
            catch (Exception exception)
            {
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

        // Do not dispose the semaphore while callers may
        // still be waiting to acquire it.
    }
    
}
