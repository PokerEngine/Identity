using Domain.Event;
using Domain.Exception;
using Domain.ValueObject;

namespace Domain.Entity;

public class Identity
{
    public AccountUid Uid { get; }
    public EncryptedPassword? EncryptedPassword { get; private set; }
    public DateTime CreatedAt { get; init; }

    private readonly List<IEvent> _events;

    private Identity(
        AccountUid uid,
        EncryptedPassword encryptedPassword,
        DateTime createdAt
    )
    {
        Uid = uid;
        EncryptedPassword = encryptedPassword;
        CreatedAt = createdAt;
        _events = [];
    }

    public static Identity FromScratch(
        AccountUid uid,
        EncryptedPassword encryptedPassword
    )
    {
        var now = DateTime.UtcNow;
        var identity = new Identity(
            uid: uid,
            encryptedPassword: encryptedPassword,
            createdAt: now
        );

        var @event = new IdentityCreatedEvent
        {
            EncryptedPassword = encryptedPassword,
            OccurredAt = now
        };
        identity.AddEvent(@event);

        return identity;
    }

    public static Identity FromEvents(AccountUid uid, List<IEvent> events)
    {
        if (events.Count == 0 || events[0] is not IdentityCreatedEvent)
        {
            throw new InvalidIdentityStateException("The first event must be an IdentityCreatedEvent");
        }

        var createdEvent = (IdentityCreatedEvent)events[0];
        var identity = new Identity(
            uid: uid,
            encryptedPassword: createdEvent.EncryptedPassword,
            createdAt: createdEvent.OccurredAt
        );

        foreach (var @event in events[1..])
        {
            switch (@event)
            {
                case PasswordChangedEvent e:
                    identity.ChangePassword(e.EncryptedPassword);
                    break;
                default:
                    throw new InvalidIdentityStateException($"{@event.GetType().Name} is not supported");
            }
        }

        identity.PullEvents();

        return identity;
    }

    public void ChangePassword(EncryptedPassword encryptedPassword)
    {
        EncryptedPassword = encryptedPassword;
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
