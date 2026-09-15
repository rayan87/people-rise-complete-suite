using Microsoft.Extensions.DependencyInjection;

namespace PeopleRise.SharedKernel;

// A minimal, provider-neutral event contract (Core Spec §11.3: "the core publishes and never
// subscribes to a product... an uninstalled product simply has no subscriber"). No MediatR, same
// philosophy as Handlers.cs: plain DI-resolved types, scanned and registered automatically.

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent e, CancellationToken ct);
}

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent e, CancellationToken ct = default) where TEvent : IDomainEvent;
}

/// <summary>Resolves every registered <see cref="IEventHandler{TEvent}"/> for the event's type and
/// invokes each. Zero registered handlers today for every Core event - that is the correct default,
/// not a bug: publishing into the void is exactly what "no subscriber" means until a product
/// actually wires one up.</summary>
public sealed class EventPublisher(IServiceProvider services) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent e, CancellationToken ct = default) where TEvent : IDomainEvent
    {
        var handlers = services.GetServices(typeof(IEventHandler<TEvent>));
        foreach (var handler in handlers)
            await ((IEventHandler<TEvent>)handler!).Handle(e, ct);
    }
}
