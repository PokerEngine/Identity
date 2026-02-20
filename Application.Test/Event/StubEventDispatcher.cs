using Application.Event;
using Domain.Event;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Application.Test.Event;

public class StubEventDispatcher : IEventDispatcher
{
    private readonly ConcurrentDictionary<AccountUid, List<IEvent>> _mapping = new();

    public Task DispatchAsync(IEvent @event, EventContext context)
    {
        var items = _mapping.GetOrAdd(context.AccountUid, _ => new List<IEvent>());
        lock (items)
            items.Add(@event);

        return Task.CompletedTask;
    }

    public Task<List<IEvent>> GetDispatchedEvents(AccountUid accountUid)
    {
        if (!_mapping.TryGetValue(accountUid, out var events))
        {
            return Task.FromResult(new List<IEvent>());
        }

        List<IEvent> snapshot;
        lock (events)
            snapshot = events.ToList();

        return Task.FromResult(snapshot);
    }

    public Task ClearDispatchedEvents(AccountUid accountUid)
    {
        if (_mapping.TryGetValue(accountUid, out var items))
        {
            lock (items)
            {
                items.Clear();
            }
        }

        return Task.CompletedTask;
    }
}
