
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

        var nowUtc = DateTimeOffset.UtcNow;
        var lockedUntilUtc = nowUtc.Add(leaseDuration);
        var lockToken = Guid.NewGuid();

        // Atomically select and claim the available messages.
        var messages = await dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                ;WITH [MessagesToClaim] AS
                (
                    SELECT TOP ({batchSize}) *
                    FROM [dbo].[OutboxMessages]
                        WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE [ProcessedOnUtc] IS NULL
                        AND
                        (
                            [LockedUntilUtc] IS NULL
                            OR [LockedUntilUtc] <= {nowUtc}
                        )
                    ORDER BY [OccurredOnUtc] ASC, [Id] ASC
                )
                UPDATE [MessagesToClaim]
                SET
                    [LockedUntilUtc] = {lockedUntilUtc},
                    [LockToken] = {lockToken}
                OUTPUT INSERTED.*;
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return messages;
    }
}
