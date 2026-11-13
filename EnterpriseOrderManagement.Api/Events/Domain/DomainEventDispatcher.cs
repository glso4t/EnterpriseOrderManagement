using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Api.Events.Domain;

public sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider)
    : IDomainEventDispatcher
{
    //Ο dispatcher βρίσκει όλους τους registered handlers για τον συγκεκριμένο τύπο event και τους εκτελεί
    public async Task DispatchAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = serviceProvider
            .GetServices<IDomainEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(
                domainEvent,
                cancellationToken);
        }
    }
}