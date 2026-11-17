namespace EnterpriseOrderManagement.Api.Events.Domain;

public sealed record OrderCancelledDomainEvent(
    int OrderId,
    int CustomerId,
    decimal TotalAmount,
    DateTimeOffset OccurredOnUtc
) : IDomainEvent;