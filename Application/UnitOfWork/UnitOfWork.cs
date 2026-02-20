using Application.Event;
using Application.Repository;
using Domain.Entity;

namespace Application.UnitOfWork;

public class UnitOfWork(
    IRepository repository,
    IEventDispatcher eventDispatcher
) : IUnitOfWork
{
    private readonly HashSet<Identity> _identities = [];

    public void Register(Identity identity)
    {
        _identities.Add(identity);
    }

    public async Task CommitAsync()
    {
        foreach (var identity in _identities)
        {
            var events = identity.PullEvents();

            if (events.Count == 0)
            {
                continue;
            }

            await repository.AddEventsAsync(identity.AccountUid, events);

            var context = new EventContext { AccountUid = identity.AccountUid };
            foreach (var @event in events)
            {
                await eventDispatcher.DispatchAsync(@event, context);
            }
        }

        _identities.Clear();
    }
}
