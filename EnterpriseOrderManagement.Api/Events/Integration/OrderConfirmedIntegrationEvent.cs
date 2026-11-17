namespace EnterpriseOrderManagement.Api.Events.Integration;

public sealed record OrderConfirmedIntegrationEvent(
    Guid EventId,
    int OrderId,
    int CustomerId,
    CustomerSnapshot Customer,
    decimal TotalAmount,
    DateTimeOffset OccurredOnUtc,
    int SchemaVersion
);