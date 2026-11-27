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

    private const int AbandonedClaimWarningThreshold = 10;

    private readonly OutboxCircuitBreaker _circuitBreaker =
        new(
            TimeProvider.System,
            failureThreshold: 3,
            breakDuration: TimeSpan.FromSeconds(30));

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

    // Claims and processes batches until the backlog is drained
    // or the circuit breaker pauses publishing.
    private async Task PollPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Do not claim messages while the circuit is open.
            var circuitState = _circuitBreaker.State;

            if (circuitState == OutboxCircuitState.Open)
            {
                logger.LogDebug(
                    "Outbox polling skipped because the circuit is open.");

                return;
            }

            // During Half-Open, allow only one probe message.
            int effectiveBatchSize =
                circuitState == OutboxCircuitState.HalfOpen
                    ? 1
                    : BatchSize;

            await using var scope = scopeFactory.CreateAsyncScope();

            var claimService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageClaimService>();

            var completionService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageCompletionService>();

            var publisher = scope.ServiceProvider
                .GetRequiredService<IIntegrationEventPublisher>();

            var claimedMessages = await claimService
                .ClaimPendingMessagesAsync(
                    effectiveBatchSize,
                    LeaseDuration,
                    cancellationToken);

            if (claimedMessages.Count == 0)
            {
                logger.LogDebug(
                    "No claimable Outbox messages found.");

                return;
            }

            logger.LogInformation(
                "Outbox Publisher claimed {MessageCount} message(s). " +
                "Circuit state: {CircuitState}.",
                claimedMessages.Count,
                circuitState);

            await ProcessClaimedBatchAsync(
                claimedMessages,
                publisher,
                completionService,
                cancellationToken);

            // Stop draining the backlog if the circuit is not closed.
            // If a Half-Open probe was successful, the circuit is closed
            // and normal processing may continue.
            if (_circuitBreaker.State != OutboxCircuitState.Closed)
            {
                return;
            }

            if (claimedMessages.Count < effectiveBatchSize)
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

        // Once the circuit opens during a batch, do not publish
        // any of its remaining messages.
        bool deferRemainingMessages = false;

        try
        {
            foreach (var message in claimedMessages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    void StopLeaseRenewal()
                    {
                        activeClaims.TryRemove(message.Id, out _);
                    }

                    if (deferRemainingMessages ||
                        !_circuitBreaker.CanAttempt)
                    {
                        deferRemainingMessages = true;

                        StopLeaseRenewal();

                        var released =
                            await completionService.ReleaseClaimAsync(
                                message.Id,
                                message.LockToken!.Value,
                                cancellationToken);

                        if (released)
                        {
                            logger.LogInformation(
                                "Released claim for Outbox message {MessageId} " +
                                "because the circuit is open.",
                                message.Id);
                        }
                        else
                        {
                            logger.LogWarning(
                                "Could not release claim for Outbox message " +
                                "{MessageId}. Its lease may have been lost.",
                                message.Id);
                        }

                        continue;
                    }

                    await ProcessMessageAsync(
                        message,
                        publisher,
                        completionService,
                        StopLeaseRenewal,
                        cancellationToken);

                    if (_circuitBreaker.State !=
                        OutboxCircuitState.Closed)
                    {
                        deferRemainingMessages = true;
                    }
                }
                finally
                {
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
                        "Lease for Outbox message {MessageId} could not be renewed. " +
                        "The claim may have expired or changed owner.",
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
        var lockToken = message.LockToken!.Value;

        if (message.AbandonedClaimCount >=
            AbandonedClaimWarningThreshold)
        {
            logger.LogWarning(
                "Outbox message {MessageId} has {AbandonedClaimCount} " +
                "abandoned claims and requires investigation.",
                message.Id,
                message.AbandonedClaimCount);
        }

        try
        {
            await publisher.PublishAsync(
                message,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Application shutdown is not a publishing failure.
            throw;
        }
        catch (Exception exception)
        {
            var failureKind =
                OutboxFailureClassifier.Classify(exception);

            var previousCircuitState =
                _circuitBreaker.State;

            _circuitBreaker.RecordFailure(failureKind);

            if (previousCircuitState != OutboxCircuitState.Open &&
                _circuitBreaker.State == OutboxCircuitState.Open)
            {
                logger.LogWarning(
                    "Outbox circuit opened after publishing failures. " +
                    "New claims are temporarily paused.");
            }

            if (failureKind == OutboxFailureKind.Unknown)
            {
                logger.LogError(
                    exception,
                    "Unclassified publishing failure for Outbox " +
                    "message {MessageId}. Attempt: {AttemptCount}.",
                    message.Id,
                    message.AttemptCount);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Failed to publish Outbox message {MessageId}. " +
                    "Failure kind: {FailureKind}. " +
                    "Attempt: {AttemptCount}. " +
                    "Previous message failures: {MessageFailureCount}.",
                    message.Id,
                    failureKind,
                    message.AttemptCount,
                    message.MessageFailureCount);
            }

            stopLeaseRenewal();

            var markedFailed =
                await completionService.MarkFailedAsync(
                    message.Id,
                    lockToken,
                    message.AttemptCount,
                    failureKind,
                    exception.Message,
                    cancellationToken);

            if (!markedFailed)
            {
                logger.LogWarning(
                    "Could not record failure for Outbox message " +
                    "{MessageId} because its lease was lost.",
                    message.Id);

                return;
            }

            if (failureKind == OutboxFailureKind.Message &&
                message.MessageFailureCount + 1 >=
                    OutboxMessageCompletionService.MaxMessageFailures)
            {
                logger.LogError(
                    "Outbox message {MessageId} was quarantined " +
                    "after {MessageFailureCount} message failures.",
                    message.Id,
                    message.MessageFailureCount + 1);
            }

            return;
        }

        // A successful confirmed publish provides evidence
        // that the RabbitMQ communication path is working.
        var circuitWasHalfOpen =
            _circuitBreaker.State == OutboxCircuitState.HalfOpen;

        _circuitBreaker.RecordSuccess();

        if (circuitWasHalfOpen)
        {
            logger.LogInformation(
                "Outbox circuit closed after successful probe publishing.");
        }

        stopLeaseRenewal();

        var marked = await completionService.MarkProcessedAsync(
            message.Id,
            lockToken,
            cancellationToken);

        if (!marked)
        {
            logger.LogWarning(
                "Outbox message {MessageId} was published but its " +
                "lease was lost. It may be published again.",
                message.Id);
        }
    }
}