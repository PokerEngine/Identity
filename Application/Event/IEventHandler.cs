using Domain.Event;

namespace Application.Event;

public interface IEventHandler<in T> where T : IEvent
{
    Task HandleAsync(T @event);
}
