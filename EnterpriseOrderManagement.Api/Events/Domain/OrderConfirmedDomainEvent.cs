namespace EnterpriseOrderManagement.Api.Events.Domain;

public sealed record OrderConfirmedDomainEvent(
    int OrderId,
    int CustomerId,
    decimal TotalAmount,
    DateTimeOffset OccurredOnUtc
) : IDomainEvent;