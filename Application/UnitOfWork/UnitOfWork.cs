using Application.Event;
using Application.Repository;
using Domain.Entity;

namespace Application.UnitOfWork;

public class UnitOfWork(
    IIdentityRepository identityRepository,
    ISessionRepository sessionRepository,
    IEventDispatcher eventDispatcher
) : IUnitOfWork
{
    private readonly HashSet<Identity> _identities = [];
    private readonly HashSet<Session> _sessions = [];

    public void RegisterIdentity(Identity identity)
    {
        _identities.Add(identity);
    }

    public void RegisterSession(Session session)
    {
        _sessions.Add(session);
    }

    public async Task CommitAsync()
    {
        await CommitIdentityAsync();
        await CommitSessionAsync();
    }

    private async Task CommitIdentityAsync()
    {
        foreach (var identity in _identities)
        {
            var events = identity.PullEvents();

            if (events.Count == 0)
            {
                continue;
            }

            await identityRepository.AddEventsAsync(identity.AccountUid, events);

            var context = new EventContext { AccountUid = identity.AccountUid };
            foreach (var @event in events)
            {
                await eventDispatcher.DispatchAsync(@event, context);
            }
        }

        _identities.Clear();
    }

    private async Task CommitSessionAsync()
    {
        foreach (var session in _sessions)
        {
            var events = session.PullEvents();

            if (events.Count == 0)
            {
                continue;
            }

            await sessionRepository.AddEventsAsync(session.Uid, events);

            var context = new EventContext { AccountUid = session.AccountUid };
            foreach (var @event in events)
            {
                await eventDispatcher.DispatchAsync(@event, context);
            }
        }

        _sessions.Clear();
    }
}
