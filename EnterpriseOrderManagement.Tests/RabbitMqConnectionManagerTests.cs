
using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Tests;

public class RabbitMqConnectionManagerTests
{
    [Fact]
    public async Task GetConnectionAsync_ReusesExistingConnection()
    {
        var (factory, connection, manager) = CreateHarness();

        await using var ownedManager = manager;

        var firstConnection =
            await manager.GetConnectionAsync();

        var secondConnection =
            await manager.GetConnectionAsync();

        Assert.Same(connection.Object, firstConnection);
        Assert.Same(firstConnection, secondConnection);

        factory.Verify(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConnectionAsync_ClosedConnection_DoesNotCreateReplacement()
    {
        var (factory, connection, manager) = CreateHarness();

        await using var ownedManager = manager;

        var originalConnection =
            await manager.GetConnectionAsync();

        // Simulate an unavailable connection.
        connection.SetupGet(
            currentConnection => currentConnection.IsOpen)
            .Returns(false);

        await Assert.ThrowsAsync<
            RabbitMqConnectionUnavailableException>(
            () => manager.GetConnectionAsync());

        // A second call must not create a replacement either.
        await Assert.ThrowsAsync<
            RabbitMqConnectionUnavailableException>(
            () => manager.GetConnectionAsync());

        factory.Verify(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Simulate successful automatic recovery.
        connection.SetupGet(
            currentConnection => currentConnection.IsOpen)
            .Returns(true);

        var recoveredConnection =
            await manager.GetConnectionAsync();

        Assert.Same(originalConnection, recoveredConnection);

        factory.Verify(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConnectionAsync_InitialFailure_CanRetry()
    {
        var (factory, connection, manager) = CreateHarness();

        factory.SetupSequence(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new TimeoutException("Initial connection failed."))
            .ReturnsAsync(connection.Object);

        await using var ownedManager = manager;

        await Assert.ThrowsAsync<TimeoutException>(
            () => manager.GetConnectionAsync());

        var recoveredConnection =
            await manager.GetConnectionAsync();

        Assert.Same(connection.Object, recoveredConnection);

        factory.Verify(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetConnectionAsync_ConcurrentCallers_CreateOneConnection()
    {
        var (factory, connection, manager) = CreateHarness();

        var creationStarted =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var allowConnectionCreation =
            new TaskCompletionSource<IConnection>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        factory.Setup(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken cancellationToken) =>
            {
                creationStarted.TrySetResult(true);

                return await allowConnectionCreation.Task
                    .WaitAsync(cancellationToken);
            });

        await using var ownedManager = manager;

        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var firstCall =
            manager.GetConnectionAsync(timeout.Token);

        await creationStarted.Task.WaitAsync(timeout.Token);

        var secondCall =
            manager.GetConnectionAsync(timeout.Token);

        Assert.False(secondCall.IsCompleted);

        allowConnectionCreation.TrySetResult(connection.Object);

        var results = await Task.WhenAll(firstCall, secondCall);

        Assert.Same(connection.Object, results[0]);
        Assert.Same(results[0], results[1]);

        factory.Verify(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_DisposesClosedConnection_OnlyOnce()
    {
        var (factory, connection, manager) = CreateHarness();

        await manager.GetConnectionAsync();

        connection.SetupGet(
            currentConnection => currentConnection.IsOpen)
            .Returns(false);

        await manager.DisposeAsync();
        await manager.DisposeAsync();

        connection.Verify(
            currentConnection => currentConnection.DisposeAsync(),
            Times.Once);

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.GetConnectionAsync());

        factory.Verify(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_BeforeFirstUse_DoesNotCreateConnection()
    {
        var (factory, _, manager) = CreateHarness();

        await manager.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.GetConnectionAsync());

        factory.Verify(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static (
        Mock<IConnectionFactory> Factory,
        Mock<IConnection> Connection,
        RabbitMqConnectionManager Manager)
        CreateHarness()
    {
        var connection = new Mock<IConnection>();

        connection.SetupGet(
            currentConnection => currentConnection.IsOpen)
            .Returns(true);

        connection.Setup(
            currentConnection => currentConnection.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var factory = new Mock<IConnectionFactory>();

        factory.Setup(
            currentFactory => currentFactory.CreateConnectionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection.Object);

        var manager = new RabbitMqConnectionManager(
            factory.Object,
            NullLogger<RabbitMqConnectionManager>.Instance);

        return (factory, connection, manager);
    }
}
