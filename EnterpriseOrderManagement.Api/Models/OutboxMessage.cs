namespace EnterpriseOrderManagement.Api.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }

    // Identifies the type of integration event.
    public string Type { get; set; } = string.Empty;

    // Serialized JSON representation of the event.
    public string Content { get; set; } = string.Empty;

    // When the event originally occurred.
    public DateTimeOffset OccurredOnUtc { get; set; }

    // Null means the message has not been published yet.
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    // Indicates when the temporary claim expires.
    public DateTimeOffset? LockedUntilUtc { get; set; }

    // Identifies the worker claim that owns this message.
    public Guid? LockToken { get; set; }

    // Number of times a worker has claimed this message.
    public int AttemptCount { get; set; }

        // Number of confirmed message-specific failures.
    public int MessageFailureCount { get; set; }

    // Number of previous claims recovered after lease expiration.
    public int AbandonedClaimCount { get; set; }

    // When the latest publishing attempt was claimed.
    public DateTimeOffset? LastAttemptOnUtc { get; set; }

    // Earliest time at which the message may be claimed again.
    public DateTimeOffset? NextAttemptOnUtc { get; set; }

    // Non-null means the message was quarantined.
    public DateTimeOffset? DeadLetteredOnUtc { get; set; }

    // Stores the last publishing error, if any.
    public string? Error { get; set; }
}