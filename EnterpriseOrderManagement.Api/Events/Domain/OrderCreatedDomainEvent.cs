namespace EnterpriseOrderManagement.Api.Events.Domain;

public sealed record OrderCreatedDomainEvent(
    int OrderId,
    int CustomerId,
    decimal TotalAmount,
    DateTimeOffset OccurredOnUtc
) : IDomainEvent;