using Application.Event;
using Domain.Event;

namespace Application.Test.Event;

public class StubEventDispatcher : IEventDispatcher
{
    private readonly List<IEvent> _events = [];

    public Task DispatchAsync(IEvent @event)
    {
        _events.Add(@event);
        return Task.CompletedTask;
    }

    public List<IEvent> GetDispatchedEvents() => _events.ToList();

    public void ClearDispatchedEvents() => _events.Clear();
}
