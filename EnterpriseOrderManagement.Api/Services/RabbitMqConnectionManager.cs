
using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.Services;

/// <summary>
/// Owns one shared RabbitMQ connection.
/// Initial connection failures are retried by callers.
/// Network recovery of an established connection is handled
/// by the RabbitMQ client.
/// </summary>
public sealed class RabbitMqConnectionManager(
    IConnectionFactory connectionFactory,
    ILogger<RabbitMqConnectionManager> logger)
    : IRabbitMqConnectionManager, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private IConnection? _connection;

    private bool _disposed;

    public async Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(RabbitMqConnectionManager));
            }

            // Reuse the existing connection.
            if (_connection is not null)
            {
                if (_connection.IsOpen)
                {
                    return _connection;
                }

                // A closed connection may be undergoing automatic
                // recovery. Do not create a second connection.
                throw new RabbitMqConnectionUnavailableException(
                    "RabbitMQ connection is currently unavailable. " +
                    "The existing connection has been retained.");
            }

            logger.LogInformation(
                "Creating the initial RabbitMQ connection.");

            // If this fails, _connection remains null and a later
            // call can retry the initial connection attempt.
            _connection = await connectionFactory
                .CreateConnectionAsync(cancellationToken);

            logger.LogInformation(
                "RabbitMQ connection established.");

            return _connection;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var connection = _connection;
            _connection = null;

            if (connection is null)
            {
                return;
            }

            try
            {
                if (connection.IsOpen)
                {
                    await connection.CloseAsync();
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not gracefully close the RabbitMQ connection.");
            }
            finally
            {
                // Dispose even when the connection is closed,
                // recovering or graceful close has failed.
                await connection.DisposeAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }

        // Do not dispose the semaphore while other callers may
        // still be waiting to acquire it.
    }
}
