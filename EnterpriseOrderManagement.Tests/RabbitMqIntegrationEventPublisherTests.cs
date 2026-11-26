using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Tests;

public class RabbitMqIntegrationEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_ReusesOneChannel_AndKeepsPublisherConfirmsEnabled()
    {
        var harness = CreateHarness();

        await using var publisher = harness.Publisher;

        // The channel is created lazily: constructing the publisher alone
        // must not establish a RabbitMQ channel.
        harness.Connection.Verify(
            connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        await publisher.PublishAsync(CreateMessage("orders.created.v1"));
        await publisher.PublishAsync(CreateMessage("orders.confirmed.v1"));

        harness.ConnectionManager.Verify(
            manager => manager.GetConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        harness.Connection.Verify(
            connection => connection.CreateChannelAsync(
                It.Is<CreateChannelOptions>(options =>
                    options.PublisherConfirmationsEnabled &&
                    options.PublisherConfirmationTrackingEnabled),
                It.IsAny<CancellationToken>()),
            Times.Once);

        harness.Channel.Verify(
            channel => channel.BasicPublishAsync<BasicProperties>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAsync_SerializesConcurrentPublishesOnSharedChannel()
    {
        var harness = CreateHarness();

        var firstPublishEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstPublish = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var activePublishes = 0;
        var maximumConcurrentPublishes = 0;
        var publishCount = 0;

        async Task CompletePublishAsync(CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref activePublishes);

            UpdateMaximum(
                ref maximumConcurrentPublishes,
                active);

            var currentPublish = Interlocked.Increment(ref publishCount);

            try
            {
                if (currentPublish == 1)
                {
                    firstPublishEntered.TrySetResult(true);
                    await releaseFirstPublish.Task.WaitAsync(
                        cancellationToken);
                }
            }
            finally
            {
                Interlocked.Decrement(ref activePublishes);
            }
        }

        harness.Channel
            .Setup(channel => channel.BasicPublishAsync<BasicProperties>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                string exchange,
                string routingKey,
                bool mandatory,
                BasicProperties basicProperties,
                ReadOnlyMemory<byte> body,
                CancellationToken cancellationToken) =>
                new ValueTask(CompletePublishAsync(cancellationToken)));

        await using var publisher = harness.Publisher;

        var firstPublish = publisher.PublishAsync(
            CreateMessage("orders.created.v1"));

        await firstPublishEntered.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        var secondPublishStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var secondPublish = Task.Run(async () =>
        {
            secondPublishStarted.TrySetResult(true);
            await publisher.PublishAsync(
                CreateMessage("orders.confirmed.v1"));
        });

        await secondPublishStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        // If the shared channel were used concurrently, the second publish
        // would complete while the first confirmation is still being held.
        var completedTask = await Task.WhenAny(
            secondPublish,
            Task.Delay(TimeSpan.FromMilliseconds(100)));

        Assert.NotSame(secondPublish, completedTask);

        releaseFirstPublish.TrySetResult(true);

        await Task.WhenAll(firstPublish, secondPublish);

        Assert.Equal(2, publishCount);
        Assert.Equal(1, maximumConcurrentPublishes);
    }

    [Fact]
    public async Task DisposeAsync_WaitsForInFlightPublish_ThenClosesChannelOnce()
    {
        var harness = CreateHarness();
        var publisher = harness.Publisher;

        var publishEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowConfirmation = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task HoldPublishUntilConfirmedAsync(
            CancellationToken cancellationToken)
        {
            publishEntered.TrySetResult(true);
            await allowConfirmation.Task.WaitAsync(cancellationToken);
        }

        harness.Channel
            .Setup(channel => channel.BasicPublishAsync<BasicProperties>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                string exchange,
                string routingKey,
                bool mandatory,
                BasicProperties basicProperties,
                ReadOnlyMemory<byte> body,
                CancellationToken cancellationToken) =>
                new ValueTask(HoldPublishUntilConfirmedAsync(cancellationToken)));

        var publishTask = publisher.PublishAsync(
            CreateMessage("orders.created.v1"));

        await publishEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var disposeTask = publisher.DisposeAsync().AsTask();

        // Disposal must wait until the publisher has released the channel.
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        Assert.False(disposeTask.IsCompleted);

        allowConfirmation.TrySetResult(true);

        await publishTask;
        await disposeTask;
        await publisher.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await publisher.PublishAsync(
                CreateMessage("orders.created.v1")));

        harness.Channel.Verify(
            channel => channel.CloseAsync(
                200,
                "Publisher is shutting down.",
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);

        harness.Channel.Verify(
            channel => channel.DisposeAsync(),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_EmptyType_RejectsMessageBeforeConnecting()
    {
        var harness = CreateHarness();

        await using var publisher = harness.Publisher;

        var message = CreateMessage(" ");

        await Assert.ThrowsAsync<
            InvalidIntegrationMessageException>(
            () => publisher.PublishAsync(message));

        harness.ConnectionManager.Verify(
            manager => manager.GetConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishAsync_InvalidJson_RejectsMessageBeforeConnecting()
    {
        var harness = CreateHarness();

        await using var publisher = harness.Publisher;

        string[] invalidContents =
        [
            "",
            " ",
            "{\"orderId\":}"
        ];

        foreach (var content in invalidContents)
        {
            var message = CreateMessage(
                "orders.order-created.v2");

            message.Content = content;

            await Assert.ThrowsAsync<
                InvalidIntegrationMessageException>(
                () => publisher.PublishAsync(message));
        }

        harness.ConnectionManager.Verify(
            manager => manager.GetConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishAsync_OversizedRoutingKey_RejectsMessageBeforeConnecting()
    {
        var harness = CreateHarness();

        await using var publisher = harness.Publisher;

        // Each Greek alpha occupies two UTF-8 bytes.
        // 128 characters result in 256 bytes.
        var oversizedType = new string('α', 128);

        var message = CreateMessage(oversizedType);

        await Assert.ThrowsAsync<
            InvalidIntegrationMessageException>(
            () => publisher.PublishAsync(message));

        harness.ConnectionManager.Verify(
            manager => manager.GetConnectionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    private static PublisherHarness CreateHarness()
    {
        var channel = new Mock<IChannel>();

        channel.SetupGet(currentChannel => currentChannel.IsOpen)
            .Returns(true);

        channel.Setup(currentChannel => currentChannel.BasicPublishAsync<BasicProperties>(
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

        channel.Setup(currentChannel => currentChannel.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var connection = new Mock<IConnection>();

        connection.Setup(currentConnection => currentConnection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object);

        var connectionManager = new Mock<IRabbitMqConnectionManager>();

        connectionManager.Setup(manager => manager.GetConnectionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection.Object);

        var publisher = new RabbitMqIntegrationEventPublisher(
            connectionManager.Object,
            NullLogger<RabbitMqIntegrationEventPublisher>.Instance);

        return new PublisherHarness(
            publisher,
            connectionManager,
            connection,
            channel);
    }

    private static OutboxMessage CreateMessage(string type)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow
        };
    }

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        while (true)
        {
            var currentMaximum = Volatile.Read(ref maximum);

            if (candidate <= currentMaximum)
            {
                return;
            }

            if (Interlocked.CompareExchange(
                ref maximum,
                candidate,
                currentMaximum) == currentMaximum)
            {
                return;
            }
        }
    }

    private sealed record PublisherHarness(
        RabbitMqIntegrationEventPublisher Publisher,
        Mock<IRabbitMqConnectionManager> ConnectionManager,
        Mock<IConnection> Connection,
        Mock<IChannel> Channel);
}