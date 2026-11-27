-- Lists quarantined Outbox messages for investigation.
-- This script does not modify any data.

SELECT
    [Id],
    [Type],
    [OccurredOnUtc],
    [AttemptCount],
    [MessageFailureCount],
    [AbandonedClaimCount],
    [LastAttemptOnUtc],
    [DeadLetteredOnUtc],
    [Error]
FROM [dbo].[OutboxMessages]
WHERE [DeadLetteredOnUtc] IS NOT NULL
    AND [ProcessedOnUtc] IS NULL
ORDER BY [DeadLetteredOnUtc] DESC;