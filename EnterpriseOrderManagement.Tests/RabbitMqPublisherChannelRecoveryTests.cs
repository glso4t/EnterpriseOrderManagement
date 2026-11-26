
using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Tests;

public class RabbitMqPublisherChannelRecoveryTests
{
    [Fact]
    public async Task PublishAsync_ClosedChannel_CreatesReplacement()
    {
        var harness = CreateHarness();

        await using var publisher = harness.Publisher;

        await publisher.PublishAsync(CreateMessage());

        harness.FirstChannel
            .SetupGet(channel => channel.IsOpen)
            .Returns(false);

        await publisher.PublishAsync(CreateMessage());

        VerifyRetired(harness.FirstChannel);

        harness.Connection.Verify(
            connection => connection.CreateChannelAsync(
                It.Is<CreateChannelOptions>(options =>
                    options.PublisherConfirmationsEnabled &&
                    options.PublisherConfirmationTrackingEnabled),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        VerifyPublished(harness.FirstChannel, Times.Once());
        VerifyPublished(harness.SecondChannel, Times.Once());
    }

    [Fact]
    public async Task PublishAsync_ConnectionUnavailable_DoesNotReplaceChannel()
    {
        var harness = CreateHarness();

        await using var publisher = harness.Publisher;

        await publisher.PublishAsync(CreateMessage());

        harness.FirstChannel
            .SetupGet(channel => channel.IsOpen)
            .Returns(false);

        harness.ConnectionManager
            .Setup(manager => manager.GetConnectionAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new RabbitMqConnectionUnavailableException(
                    "Connection unavailable."));

        await Assert.ThrowsAsync<
            RabbitMqConnectionUnavailableException>(
            () => publisher.PublishAsync(CreateMessage()));

        harness.Connection.Verify(
            connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        harness.FirstChannel.Verify(
            channel => channel.DisposeAsync(),
            Times.Never);

        VerifyPublished(harness.FirstChannel, Times.Once());
        VerifyPublished(harness.SecondChannel, Times.Never());
    }

    [Fact]
    public async Task PublishAsync_AutomaticallyRecoveredChannel_IsReused()
    {
        var harness = CreateHarness();

        await using var publisher = harness.Publisher;

        await publisher.PublishAsync(CreateMessage());

        harness.FirstChannel
            .SetupGet(channel => channel.IsOpen)
            .Returns(false);

        harness.ConnectionManager
            .Setup(manager => manager.GetConnectionAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new RabbitMqConnectionUnavailableException(
                    "Connection unavailable."));

        await Assert.ThrowsAsync<
            RabbitMqConnectionUnavailableException>(
            () => publisher.PublishAsync(CreateMessage()));

        // Simulate successful automatic recovery.
        harness.ConnectionManager
            .Setup(manager => manager.GetConnectionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(harness.Connection.Object);

        harness.FirstChannel
            .SetupGet(channel => channel.IsOpen)
            .Returns(true);

        await publisher.PublishAsync(CreateMessage());

        harness.Connection.Verify(
            connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        harness.FirstChannel.Verify(
            channel => channel.DisposeAsync(),
            Times.Never);

        VerifyPublished(
            harness.FirstChannel,
            Times.Exactly(2));

        VerifyPublished(
            harness.SecondChannel,
            Times.Never());
    }

    [Fact]
    public async Task PublishAsync_ReplacementCreationFails_CanRetryLater()
    {
        var harness = CreateHarness();

        harness.Connection.SetupSequence(
            connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(harness.FirstChannel.Object)
            .ThrowsAsync(
                new TimeoutException("Channel creation failed."))
            .ReturnsAsync(harness.SecondChannel.Object);

        await using var publisher = harness.Publisher;

        await publisher.PublishAsync(CreateMessage());

        harness.FirstChannel
            .SetupGet(channel => channel.IsOpen)
            .Returns(false);

        var message = CreateMessage();

        await Assert.ThrowsAsync<TimeoutException>(
            () => publisher.PublishAsync(message));

        // The next call represents a later Outbox attempt.
        await publisher.PublishAsync(message);

        VerifyRetired(harness.FirstChannel);

        harness.Connection.Verify(
            connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        VerifyPublished(harness.SecondChannel, Times.Once());
    }

    [Fact]
    public async Task PublishAsync_ConcurrentCallers_CreateOneReplacementChannel()
    {
        var harness = CreateHarness();

        await using var publisher = harness.Publisher;

        await publisher.PublishAsync(CreateMessage());

        harness.FirstChannel
            .SetupGet(channel => channel.IsOpen)
            .Returns(false);

        var creationStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var allowCreation = new TaskCompletionSource<IChannel>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        harness.Connection
            .Setup(connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (
                CreateChannelOptions? options,
                CancellationToken cancellationToken) =>
            {
                creationStarted.TrySetResult(true);

                return await allowCreation.Task.WaitAsync(
                    cancellationToken);
            });

        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(4));

        var firstPublish = publisher.PublishAsync(
            CreateMessage(),
            timeout.Token);

        await creationStarted.Task.WaitAsync(timeout.Token);

        var secondPublish = publisher.PublishAsync(
            CreateMessage(),
            timeout.Token);

        try
        {
            Assert.False(secondPublish.IsCompleted);
        }
        finally
        {
            allowCreation.TrySetResult(
                harness.SecondChannel.Object);
        }

        await Task.WhenAll(firstPublish, secondPublish);

        VerifyRetired(harness.FirstChannel);

        harness.Connection.Verify(
            connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        VerifyPublished(
            harness.SecondChannel,
            Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAsync_PublishFails_DoesNotRetryInternally()
    {
        var harness = CreateHarness();

        var message = CreateMessage();

        harness.FirstChannel
            .Setup(channel => channel.BasicPublishAsync<BasicProperties>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                harness.FirstChannel
                    .SetupGet(channel => channel.IsOpen)
                    .Returns(false);

                return new ValueTask(
                    Task.FromException(
                        new InvalidOperationException(
                            "Simulated channel failure.")));
            });

        await using var publisher = harness.Publisher;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(message));

        // The publisher must not immediately resend the message.
        VerifyPublished(harness.FirstChannel, Times.Once());
        VerifyPublished(harness.SecondChannel, Times.Never());

        harness.Connection.Verify(
            connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Simulate the later Outbox retry.
        await publisher.PublishAsync(message);

        VerifyRetired(harness.FirstChannel);

        harness.SecondChannel.Verify(
            channel => channel.BasicPublishAsync<BasicProperties>(
                It.IsAny<string>(),
                message.Type,
                true,
                It.Is<BasicProperties>(properties =>
                    properties.MessageId == message.Id.ToString()),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Mock<IChannel> CreateChannelMock()
    {
        var channel = new Mock<IChannel>();

        channel.SetupGet(currentChannel => currentChannel.IsOpen)
            .Returns(true);

        channel.Setup(currentChannel =>
                currentChannel.BasicPublishAsync<BasicProperties>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        channel.Setup(currentChannel => currentChannel.CloseAsync(
                It.IsAny<ushort>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channel.Setup(currentChannel =>
                currentChannel.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        return channel;
    }

    private static RecoveryHarness CreateHarness()
    {
        var firstChannel = CreateChannelMock();
        var secondChannel = CreateChannelMock();

        var connection = new Mock<IConnection>();

        connection.SetupGet(
                currentConnection => currentConnection.IsOpen)
            .Returns(true);

        connection.SetupSequence(
                currentConnection => currentConnection.CreateChannelAsync(
                    It.IsAny<CreateChannelOptions>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstChannel.Object)
            .ReturnsAsync(secondChannel.Object);

        var connectionManager =
            new Mock<IRabbitMqConnectionManager>();

        connectionManager.Setup(
                manager => manager.GetConnectionAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection.Object);

        var publisher = new RabbitMqIntegrationEventPublisher(
            connectionManager.Object,
            NullLogger<RabbitMqIntegrationEventPublisher>.Instance);

        return new RecoveryHarness(
            publisher,
            connectionManager,
            connection,
            firstChannel,
            secondChannel);
    }

    private static void VerifyRetired(Mock<IChannel> channel)
    {
        channel.Verify(
            currentChannel => currentChannel.CloseAsync(
                200,
                "Replacing unavailable publisher channel.",
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);

        channel.Verify(
            currentChannel => currentChannel.DisposeAsync(),
            Times.Once);
    }

    private static void VerifyPublished(
        Mock<IChannel> channel,
        Times times)
    {
        channel.Verify(
            currentChannel =>
                currentChannel.BasicPublishAsync<BasicProperties>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    true,
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()),
            times);
    }

    private static OutboxMessage CreateMessage()
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "orders.order-created.v2",
            Content = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow
        };
    }

    private sealed record RecoveryHarness(
        RabbitMqIntegrationEventPublisher Publisher,
        Mock<IRabbitMqConnectionManager> ConnectionManager,
        Mock<IConnection> Connection,
        Mock<IChannel> FirstChannel,
        Mock<IChannel> SecondChannel);
}
