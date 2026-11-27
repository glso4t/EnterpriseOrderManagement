-- Requeues one quarantined Outbox message.
-- Investigate and resolve the original failure before execution.
-- Replace NULL with the actual Outbox message ID.

SET NOCOUNT ON;

DECLARE @MessageId uniqueidentifier = NULL;

IF @MessageId IS NULL
BEGIN
    ;THROW 50001, 'A valid Outbox message ID must be provided.', 1;
END;

UPDATE [dbo].[OutboxMessages]
SET
    [DeadLetteredOnUtc] = NULL,
    [AttemptCount] = 0,
    [MessageFailureCount] = 0,
    [AbandonedClaimCount] = 0,
    [NextAttemptOnUtc] = NULL,
    [LastAttemptOnUtc] = NULL,
    [Error] = NULL,
    [LockToken] = NULL,
    [LockedUntilUtc] = NULL
WHERE [Id] = @MessageId
    AND [DeadLetteredOnUtc] IS NOT NULL
    AND [ProcessedOnUtc] IS NULL
    AND [LockToken] IS NULL
    AND [LockedUntilUtc] IS NULL;

IF @@ROWCOUNT <> 1
BEGIN
    ;THROW 50002, 'No eligible quarantined message was found.', 1;
END;

SELECT
    [Id],
    [Type],
    [AttemptCount],
    [MessageFailureCount],
    [AbandonedClaimCount],
    [NextAttemptOnUtc],
    [DeadLetteredOnUtc]
FROM [dbo].[OutboxMessages]
WHERE [Id] = @MessageId;