
using EnterpriseOrderManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

public sealed class OutboxMessageLeaseRenewalService(
    EnterpriseOrderManagementDbContext dbContext)
{
    public async Task<bool> RenewLeaseAsync(
        Guid messageId,
        Guid lockToken,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        // SQL Server DATEADD(MILLISECOND, ...) accepts an Int32 value.
        var leaseMilliseconds = checked(
            (int)Math.Ceiling(leaseDuration.TotalMilliseconds));

        if (leaseMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        // The message must still be owned by this worker and its current
        // lease must not have expired. A stale worker cannot resurrect a lease.
        var updatedRows = await dbContext.Database
            .ExecuteSqlInterpolatedAsync($"""
                UPDATE [dbo].[OutboxMessages]
                SET [LockedUntilUtc] = DATEADD(
                    MILLISECOND,
                    {leaseMilliseconds},
                    TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'))
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
}
