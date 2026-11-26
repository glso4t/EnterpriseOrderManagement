using EnterpriseOrderManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

public sealed class OutboxMessageCompletionService(
    EnterpriseOrderManagementDbContext dbContext)
{
    private const int MaxErrorLength = 2000;

    public const int MaxMessageFailures = 10;

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
                    AND [LockedUntilUtc] >
                        TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00')
                    AND [ProcessedOnUtc] IS NULL
                    AND [DeadLetteredOnUtc] IS NULL;
                """,
                cancellationToken);

        return updatedRows == 1;
    }

    // Schedules retries for all failure categories.
    // Only confirmed message-specific failures consume
    // the quarantine budget.
    public async Task<bool> MarkFailedAsync(
        Guid messageId,
        Guid lockToken,
        int attemptCount,
        OutboxFailureKind failureKind,
        string error,
        CancellationToken cancellationToken)
    {
        if (attemptCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptCount));
        }

        if (!Enum.IsDefined(failureKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(failureKind));
        }

        ArgumentNullException.ThrowIfNull(error);

        var trimmedError = error.Length > MaxErrorLength
            ? error[..MaxErrorLength]
            : error;

        // Only a message-specific failure consumes the
        // message failure budget.
        int messageFailureIncrement =
            failureKind == OutboxFailureKind.Message ? 1 : 0;

        var retryDelay = CalculateRetryDelay(attemptCount);

        var retryDelayMilliseconds = checked(
            (int)retryDelay.TotalMilliseconds);

        var updatedRows = await dbContext.Database
            .ExecuteSqlInterpolatedAsync($"""
                DECLARE @NowUtc datetimeoffset(7) =
                    TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');

                UPDATE [dbo].[OutboxMessages]
                SET
                    [Error] = {trimmedError},

                    [MessageFailureCount] =
                        [MessageFailureCount] +
                        {messageFailureIncrement},

                    [NextAttemptOnUtc] = CASE
                        WHEN {messageFailureIncrement} = 1
                            AND [MessageFailureCount] +
                                {messageFailureIncrement}
                                >= {MaxMessageFailures}
                        THEN NULL

                        ELSE DATEADD(
                            MILLISECOND,
                            {retryDelayMilliseconds},
                            @NowUtc)
                    END,

                    [DeadLetteredOnUtc] = CASE
                        WHEN {messageFailureIncrement} = 1
                            AND [MessageFailureCount] +
                                {messageFailureIncrement}
                                >= {MaxMessageFailures}
                        THEN @NowUtc

                        ELSE NULL
                    END,

                    [LockedUntilUtc] = NULL,
                    [LockToken] = NULL

                WHERE [Id] = {messageId}
                    AND [LockToken] = {lockToken}
                    AND [LockedUntilUtc] > @NowUtc
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

        // Add +/-20% jitter.
        var jitterFactor =
            0.8 + (Random.Shared.NextDouble() * 0.4);

        return TimeSpan.FromMilliseconds(
            Math.Min(
                cappedMilliseconds * jitterFactor,
                MaxRetryDelay.TotalMilliseconds));
    }
}