using Application.Event;
using Application.Repository;
using Domain.Entity;
using Domain.Event;

namespace Application.UnitOfWork;

public class UnitOfWork(
    IIdentityRepository identityRepository,
    ISessionRepository sessionRepository,
    IEventDispatcher eventDispatcher
) : IUnitOfWork
{
    private readonly List<Func<Task>> _commits = [];

    public void Register(Identity identity) =>
        _commits.Add(() => CommitAsync(
            identity,
            events => identityRepository.AddEventsAsync(identity.AccountUid, events)
        ));

    public void Register(Session session) =>
        _commits.Add(() => CommitAsync(
            session,
            events => sessionRepository.AddEventsAsync(session.Uid, events)
        ));

    public async Task CommitAsync()
    {
        foreach (var commit in _commits)
            await commit();
        _commits.Clear();
    }

    private async Task CommitAsync(IAggregateRoot aggregate, Func<List<IEvent>, Task> persist)
    {
        var events = aggregate.PullEvents();
        if (events.Count == 0) return;
        await persist(events);
        foreach (var @event in events)
            await eventDispatcher.DispatchAsync(@event);
    }
}
