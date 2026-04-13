using Domain.Event;

namespace Application.Event;

public interface IEventDispatcher
{
    Task DispatchAsync(IEvent @event);
}
