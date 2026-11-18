using EnterpriseOrderManagement.Api.Events.Integration;
using EnterpriseOrderManagement.Api.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

public sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(5);

    private static readonly TimeSpan LeaseDuration =
        TimeSpan.FromMinutes(1);

    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Outbox Publisher background service started.");
        using var timer = new PeriodicTimer(PollingInterval);
        try
        {
            do
            {
                try
                {
                    await PollPendingMessagesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "An error occurred while polling the Outbox.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Expected when the application is shutting down.
        }
        logger.LogInformation(
            "Outbox Publisher background service stopped.");
    }

    // Claims and processes batches until the backlog is drained.
    private async Task PollPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var claimService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageClaimService>();
            var completionService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageCompletionService>();
            var publisher = scope.ServiceProvider
                .GetRequiredService<IIntegrationEventPublisher>();
            var claimedMessages = await claimService
                .ClaimPendingMessagesAsync(
                    BatchSize,
                    LeaseDuration,
                    cancellationToken);
            if (claimedMessages.Count == 0)
            {
                logger.LogDebug(
                    "No claimable Outbox messages found.");
                return;
            }
            logger.LogInformation(
                "Outbox Publisher claimed {MessageCount} message(s).",
                claimedMessages.Count);
            foreach (var message in claimedMessages)
            {
                await ProcessMessageAsync(
                    message,
                    publisher,
                    completionService,
                    cancellationToken);
            }
            // A partial batch means the backlog is drained.
            if (claimedMessages.Count < BatchSize)
            {
                return;
            }
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IIntegrationEventPublisher publisher,
        OutboxMessageCompletionService completionService,
        CancellationToken cancellationToken)
    {
        // Every claimed message carries the token of its claim.
        var lockToken = message.LockToken!.Value;
        try
        {
            await publisher.PublishAsync(message, cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down: leave the lease to expire so that
            // another worker can retry the message.
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to publish Outbox message {MessageId}.",
                message.Id);
            await completionService.MarkFailedAsync(
                message.Id,
                lockToken,
                exception.Message,
                cancellationToken);
            return;
        }
        var marked = await completionService.MarkProcessedAsync(
            message.Id,
            lockToken,
            cancellationToken);
        if (!marked)
        {
            logger.LogWarning(
                "Outbox message {MessageId} was published but its lease was lost. It may be published again.",
                message.Id);
        }
    }
}