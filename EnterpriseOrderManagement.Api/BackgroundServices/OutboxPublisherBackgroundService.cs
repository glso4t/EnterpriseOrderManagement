
using System.Collections.Concurrent;

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

    private static readonly TimeSpan LeaseRenewalInterval =
        TimeSpan.FromTicks(LeaseDuration.Ticks / 3);

    private const int BatchSize = 5;

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

            // All messages in a claimed batch already have an active lease.
            // Renew every still-active lease while the batch is processed.
            await ProcessClaimedBatchAsync(
                claimedMessages,
                publisher,
                completionService,
                cancellationToken);

            // A partial batch means the backlog is drained.
            if (claimedMessages.Count < BatchSize)
            {
                return;
            }
        }
    }

    private async Task ProcessClaimedBatchAsync(
        IReadOnlyCollection<OutboxMessage> claimedMessages,
        IIntegrationEventPublisher publisher,
        OutboxMessageCompletionService completionService,
        CancellationToken cancellationToken)
    {
        var activeClaims = new ConcurrentDictionary<Guid, Guid>(
            claimedMessages.ToDictionary(
                message => message.Id,
                message => message.LockToken
                    ?? throw new InvalidOperationException(
                        $"Claimed Outbox message {message.Id} has no lock token.")));

        using var renewalCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        var renewalTask = RenewActiveLeasesAsync(
            activeClaims,
            renewalCts.Token);

        try
        {
            foreach (var message in claimedMessages)
            {
                try
                {
                    void StopLeaseRenewal()
                    {
                        activeClaims.TryRemove(message.Id, out _);
                    }

                    await ProcessMessageAsync(
                        message,
                        publisher,
                        completionService,
                        StopLeaseRenewal,
                        cancellationToken);
                }
                finally
                {
                    // Once completed or failed, this message no longer
                    // needs its lease renewed.
                    activeClaims.TryRemove(message.Id, out _);
                }
            }
        }
        finally
        {
            renewalCts.Cancel();

            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException)
                when (renewalCts.IsCancellationRequested)
            {
                // Expected when the batch has finished or the host stops.
            }
        }
    }

    private async Task RenewActiveLeasesAsync(
        ConcurrentDictionary<Guid, Guid> activeClaims,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(LeaseRenewalInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var leaseRenewalService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageLeaseRenewalService>();

            // Snapshot avoids iterating a collection while messages are
            // being removed from the active batch.
            foreach (var claim in activeClaims.ToArray())
            {
                bool renewed;

                try
                {
                    renewed = await leaseRenewalService.RenewLeaseAsync(
                        claim.Key,
                        claim.Value,
                        LeaseDuration,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // A transient database error is logged. The next
                    // heartbeat will try again while the lease is valid.
                    logger.LogError(
                        exception,
                        "Could not renew the lease for Outbox message {MessageId}.",
                        claim.Key);

                    continue;
                }

                if (!renewed &&
                    activeClaims.TryRemove(claim.Key, out _))
                {
                    logger.LogWarning(
                        "Lease for Outbox message {MessageId} could not be renewed. The claim may have expired or changed owner.",
                        claim.Key);
                }
            }
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IIntegrationEventPublisher publisher,
        OutboxMessageCompletionService completionService,
        Action stopLeaseRenewal,
        CancellationToken cancellationToken)
    {
        // Every claimed message carries the token of its claim.
        var lockToken = message.LockToken!.Value;

        try
        {
            await publisher.PublishAsync(
                message,
                cancellationToken);
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

            // Stop the heartbeat before releasing the claim in the database.
            stopLeaseRenewal();

            var markedFailed = await completionService.MarkFailedAsync(
                message.Id,
                lockToken,
                message.AttemptCount,
                exception.Message,
                cancellationToken);

            if (!markedFailed)
            {
                logger.LogWarning(
                    "Could not schedule retry for Outbox message {MessageId} because its lease was lost.",
                    message.Id);
            }

            return;
        }

        // Stop the heartbeat before completing and releasing the claim.
        stopLeaseRenewal();

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
