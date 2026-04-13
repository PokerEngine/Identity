using Domain.Event;
using Domain.Exception;
using Domain.ValueObject;

namespace Domain.Entity;

public class Identity : IAggregateRoot
{
    public AccountUid AccountUid { get; }
    public PasswordHash PasswordHash { get; private set; }
    public DateTime CreatedAt { get; }

    private readonly List<IEvent> _events;

    private Identity(
        AccountUid accountUid,
        PasswordHash passwordHash,
        DateTime createdAt
    )
    {
        AccountUid = accountUid;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        _events = [];
    }

    public static Identity FromScratch(
        AccountUid accountUid,
        PasswordHash passwordHash,
        DateTime now
    )
    {
        var identity = new Identity(
            accountUid: accountUid,
            passwordHash: passwordHash,
            createdAt: now
        );

        var @event = new PasswordInitializedEvent
        {
            AccountUid = accountUid,
            PasswordHash = passwordHash,
            OccurredAt = now
        };
        identity.AddEvent(@event);

        return identity;
    }

    public static Identity FromEvents(AccountUid accountUid, List<IEvent> events)
    {
        if (events.Count == 0 || events[0] is not PasswordInitializedEvent)
        {
            throw new InvalidIdentityStateException("The first event must be an PasswordInitializedEvent");
        }

        var createdEvent = (PasswordInitializedEvent)events[0];
        var identity = new Identity(
            accountUid: accountUid,
            passwordHash: createdEvent.PasswordHash,
            createdAt: createdEvent.OccurredAt
        );

        foreach (var @event in events[1..])
        {
            switch (@event)
            {
                case PasswordChangedEvent e:
                    identity.ChangePassword(e.PasswordHash, e.OccurredAt);
                    break;
                default:
                    throw new InvalidIdentityStateException($"{@event.GetType().Name} is not supported");
            }
        }

        identity.PullEvents();

        return identity;
    }

    public void ChangePassword(PasswordHash passwordHash, DateTime now)
    {
        PasswordHash = passwordHash;

        var @event = new PasswordChangedEvent
        {
            AccountUid = AccountUid,
            PasswordHash = passwordHash,
            OccurredAt = now
        };

        AddEvent(@event);
    }

    # region Events

    public List<IEvent> PullEvents()
    {
        var events = _events.ToList();
        _events.Clear();

        return events;
    }

    private void AddEvent(IEvent @event)
    {
        _events.Add(@event);
    }

    # endregion
}
