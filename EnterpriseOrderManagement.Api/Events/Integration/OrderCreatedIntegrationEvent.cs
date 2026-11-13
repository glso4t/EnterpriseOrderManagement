namespace EnterpriseOrderManagement.Api.Events.Integration;

public sealed record OrderCreatedIntegrationEvent(
    Guid EventId,
    int OrderId,
    int CustomerId,
    decimal TotalAmount,
    DateTimeOffset OccurredOnUtc,
    int SchemaVersion   //έκδοση του συμβολαίου, χρήσιμη όταν αλλάξει η μορφή του μηνύματος
);