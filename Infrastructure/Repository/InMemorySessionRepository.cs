using Application.Exception;
using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Infrastructure.Repository;

public class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<SessionUid, List<IEvent>> _mapping = new();

    public Task<SessionUid> GetNextUidAsync()
    {
        return Task.FromResult(new SessionUid(Guid.NewGuid()));
    }

    public Task<List<IEvent>> GetEventsAsync(SessionUid uid)
    {
        if (!_mapping.TryGetValue(uid, out var events))
        {
            throw new SessionNotFoundException("The session is not found");
        }

        List<IEvent> snapshot;
        lock (events)
            snapshot = events.ToList();

        return Task.FromResult(snapshot);
    }

    public Task AddEventsAsync(SessionUid uid, List<IEvent> events)
    {
        var items = _mapping.GetOrAdd(uid, _ => new List<IEvent>());
        lock (items)
            items.AddRange(events);

        return Task.CompletedTask;
    }
}
