
using EnterpriseOrderManagement.Api.Data;

using Microsoft.EntityFrameworkCore;
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


//Είναι υπεύθυνο για την αναζήτηση των εκκρεμών μηνυμάτων
    private async Task PollPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<EnterpriseOrderManagementDbContext>();

        var pendingMessages = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            logger.LogDebug(
                "No pending Outbox messages found.");
            return;
        }
        logger.LogInformation(
            "Outbox polling found {MessageCount} pending message(s).",
            pendingMessages.Count);
        foreach (var message in pendingMessages)
        {
            logger.LogDebug(
                "Pending Outbox message {MessageId} of type {MessageType}.",
                message.Id,
                message.Type);
        }
    }
}
