namespace EnterpriseOrderManagement.Api.Events.Domain;

// Represents something important that happened in the business domain.
public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}


// Every domain event handler must implement this interface.
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(
        TEvent domainEvent,
        CancellationToken cancellationToken = default);
}


// Provides a common way to dispatch domain events.
public interface IDomainEventDispatcher
{
    Task DispatchAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}