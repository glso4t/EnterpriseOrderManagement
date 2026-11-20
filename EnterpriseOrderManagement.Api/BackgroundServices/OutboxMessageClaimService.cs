
using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

public sealed class OutboxMessageClaimService(
    EnterpriseOrderManagementDbContext dbContext)
{
    public async Task<List<OutboxMessage>> ClaimPendingMessagesAsync(
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        var leaseMilliseconds = checked(
            (int)Math.Ceiling(leaseDuration.TotalMilliseconds));

        if (leaseMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        var lockToken = Guid.NewGuid();

        //SQL Server supplies the time so workers do not depend on
        //synchronized application-server clocks.
        // Atomically select and claim the available messages.
        var messages = await dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                DECLARE @NowUtc datetimeoffset(7) =
                    TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');

                ;WITH [MessagesToClaim] AS
                (
                    SELECT TOP ({batchSize}) *
                    FROM [dbo].[OutboxMessages]
                        WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE [ProcessedOnUtc] IS NULL
                        AND [DeadLetteredOnUtc] IS NULL
                        AND
                        (
                            [NextAttemptOnUtc] IS NULL
                            OR [NextAttemptOnUtc] <= @NowUtc
                        )
                        AND
                        (
                            [LockedUntilUtc] IS NULL
                            OR [LockedUntilUtc] <= @NowUtc
                        )
                    ORDER BY [OccurredOnUtc] ASC, [Id] ASC
                )
                UPDATE [MessagesToClaim]
                SET
                    [LockedUntilUtc] = DATEADD(
                        MILLISECOND,
                        {leaseMilliseconds},
                        @NowUtc),
                    [LockToken] = {lockToken},
                    [AttemptCount] = [AttemptCount] + 1,
                    [LastAttemptOnUtc] = @NowUtc
                OUTPUT INSERTED.*;
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return messages;
    }
}
