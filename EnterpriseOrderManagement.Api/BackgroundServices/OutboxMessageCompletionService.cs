using EnterpriseOrderManagement.Api.Data;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

public sealed class OutboxMessageCompletionService(
    EnterpriseOrderManagementDbContext dbContext)
{
    private const int MaxErrorLength = 2000;

    // Marks a claimed message as published.
    // Returns false when the claim no longer belongs to the caller.
    public async Task<bool> MarkProcessedAsync(
        Guid messageId,
        Guid lockToken,
        CancellationToken cancellationToken)
    {
        var processedOnUtc = DateTimeOffset.UtcNow;
        var updatedRows = await dbContext.OutboxMessages
            .Where(message =>
                message.Id == messageId &&
                message.LockToken == lockToken &&
                message.ProcessedOnUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        message => message.ProcessedOnUtc,
                        (DateTimeOffset?)processedOnUtc)
                    .SetProperty(
                        message => message.Error,
                        (string?)null),
                cancellationToken);
        return updatedRows == 1;
    }

    // Records the publishing error. The lease is kept, so the message
    // is retried automatically after the lease expires.
    public async Task<bool> MarkFailedAsync(
        Guid messageId,
        Guid lockToken,
        string error,
        CancellationToken cancellationToken)
    {
        var trimmedError = error.Length > MaxErrorLength
            ? error[..MaxErrorLength]
            : error;
        var updatedRows = await dbContext.OutboxMessages
            .Where(message =>
                message.Id == messageId &&
                message.LockToken == lockToken &&
                message.ProcessedOnUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    message => message.Error,
                    (string?)trimmedError),
                cancellationToken);
        return updatedRows == 1;
    }
}