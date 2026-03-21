using Application.Exception;
using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Application.Test.Repository;

public class StubIdentityRepository : IIdentityRepository
{
    private readonly ConcurrentDictionary<AccountUid, List<IEvent>> _mapping = new();

    public Task<bool> ExistsAsync(AccountUid accountUid)
    {
        return Task.FromResult(_mapping.ContainsKey(accountUid));
    }

    public Task<List<IEvent>> GetEventsAsync(AccountUid accountUid)
    {
        if (!_mapping.TryGetValue(accountUid, out var events))
        {
            throw new IdentityNotFoundException("The identity is not found");
        }

        List<IEvent> snapshot;
        lock (events)
            snapshot = events.ToList();

        return Task.FromResult(snapshot);
    }

    public Task AddEventsAsync(AccountUid accountUid, List<IEvent> events)
    {
        var items = _mapping.GetOrAdd(accountUid, _ => new List<IEvent>());
        lock (items)
            items.AddRange(events);

        return Task.CompletedTask;
    }
}
