using EnterpriseOrderManagement.Api.Data;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

public sealed class OutboxMessageCompletionService(
    EnterpriseOrderManagementDbContext dbContext)
{
    private const int MaxErrorLength = 2000;

    public const int MaxAttempts = 10;

    private static readonly TimeSpan BaseRetryDelay =
        TimeSpan.FromSeconds(5);

    private static readonly TimeSpan MaxRetryDelay =
        TimeSpan.FromMinutes(15);

    // Marks a claimed message as published.
    // Returns false when the claim no longer belongs to the caller.
    public async Task<bool> MarkProcessedAsync(
        Guid messageId,
        Guid lockToken,
        CancellationToken cancellationToken)
    {
        var updatedRows = await dbContext.Database
            .ExecuteSqlInterpolatedAsync($"""
                UPDATE [dbo].[OutboxMessages]
                SET
                    [ProcessedOnUtc] =
                        TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'),
                    [Error] = NULL,
                    [LockedUntilUtc] = NULL,
                    [LockToken] = NULL,
                    [NextAttemptOnUtc] = NULL
                WHERE [Id] = {messageId}
                    AND [LockToken] = {lockToken}
                    AND [ProcessedOnUtc] IS NULL
                    AND [DeadLetteredOnUtc] IS NULL;
                """,
                cancellationToken);
        return updatedRows == 1;
    }

    // Schedules a retry with exponential backoff and jitter.
    // Once the maximum number of claims is reached, the message is quarantined.
    public async Task<bool> MarkFailedAsync(
        Guid messageId,
        Guid lockToken,
        int attemptCount,
        string error,
        CancellationToken cancellationToken)
    {
        if (attemptCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptCount));
        }

        var trimmedError = error.Length > MaxErrorLength
            ? error[..MaxErrorLength]
            : error;
        var retryDelay = CalculateRetryDelay(attemptCount);
        var retryDelayMilliseconds = checked(
            (int)retryDelay.TotalMilliseconds);

        var updatedRows = await dbContext.Database
            .ExecuteSqlInterpolatedAsync($"""
                UPDATE [dbo].[OutboxMessages]
                SET
                    [Error] = {trimmedError},
                    [NextAttemptOnUtc] = CASE
                        WHEN [AttemptCount] >= {MaxAttempts}
                            THEN NULL
                        ELSE DATEADD(
                            MILLISECOND,
                            {retryDelayMilliseconds},
                            TODATETIMEOFFSET(
                                SYSUTCDATETIME(),
                                '+00:00'))
                    END,
                    [DeadLetteredOnUtc] = CASE
                        WHEN [AttemptCount] >= {MaxAttempts}
                            THEN TODATETIMEOFFSET(
                                SYSUTCDATETIME(),
                                '+00:00')
                        ELSE NULL
                    END,
                    [LockedUntilUtc] = NULL,
                    [LockToken] = NULL
                WHERE [Id] = {messageId}
                    AND [LockToken] = {lockToken}
                    AND [ProcessedOnUtc] IS NULL
                    AND [DeadLetteredOnUtc] IS NULL;
                """,
                cancellationToken);
        return updatedRows == 1;
    }

    private static TimeSpan CalculateRetryDelay(int attemptCount)
    {
        // 5s, 10s, 20s, 40s... capped at 15 minutes.
        var exponent = Math.Min(attemptCount - 1, 20);

        var baseMilliseconds =
            BaseRetryDelay.TotalMilliseconds *
            Math.Pow(2, exponent);

        var cappedMilliseconds = Math.Min(
            baseMilliseconds,
            MaxRetryDelay.TotalMilliseconds);

        // Add +/-20% jitter so many failed messages do not retry together.
        var jitterFactor =
            0.8 + (Random.Shared.NextDouble() * 0.4);

        return TimeSpan.FromMilliseconds(
            Math.Min(
                cappedMilliseconds * jitterFactor,
                MaxRetryDelay.TotalMilliseconds));
    }
}