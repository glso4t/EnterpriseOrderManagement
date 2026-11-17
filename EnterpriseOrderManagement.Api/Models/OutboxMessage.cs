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

    // Stores the last publishing error, if any.
    public string? Error { get; set; }
}