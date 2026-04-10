using Domain.Event;
using Domain.Exception;
using Domain.ValueObject;

namespace Domain.Entity;

public class Session
{
    public SessionUid Uid { get; }
    public AccountUid AccountUid { get; }
    public RefreshTokenHash RefreshTokenHash { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private readonly List<IEvent> _events;

    public bool IsRevoked => RevokedAt != null;

    private Session(
        SessionUid uid,
        AccountUid accountUid,
        RefreshTokenHash refreshTokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        DateTime? revokedAt
    )
    {
        Uid = uid;
        AccountUid = accountUid;
        RefreshTokenHash = refreshTokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        _events = [];
    }

    public static Session FromScratch(
        SessionUid uid,
        AccountUid accountUid,
        RefreshTokenHash refreshTokenHash,
        DateTime expiresAt,
        DateTime now
    )
    {
        var session = new Session(
            uid: uid,
            accountUid: accountUid,
            refreshTokenHash: refreshTokenHash,
            createdAt: now,
            expiresAt: expiresAt,
            revokedAt: null
        );

        var @event = new SessionCreatedEvent
        {
            AccountUid = accountUid,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = expiresAt,
            OccurredAt = now
        };
        session.AddEvent(@event);

        return session;
    }

    public static Session FromEvents(SessionUid uid, List<IEvent> events)
    {
        if (events.Count == 0 || events[0] is not SessionCreatedEvent)
        {
            throw new InvalidSessionStateException("The first event must be an SessionCreatedEvent");
        }

        var createdEvent = (SessionCreatedEvent)events[0];
        var session = new Session(
            uid: uid,
            accountUid: createdEvent.AccountUid,
            refreshTokenHash: createdEvent.RefreshTokenHash,
            createdAt: createdEvent.OccurredAt,
            expiresAt: createdEvent.ExpiresAt,
            revokedAt: null
        );

        foreach (var @event in events[1..])
        {
            switch (@event)
            {
                case SessionRefreshedEvent e:
                    session.Refresh(e.RefreshTokenHash, e.ExpiresAt, e.OccurredAt);
                    break;
                case SessionRevokedEvent e:
                    session.Revoke(e.OccurredAt);
                    break;
                default:
                    throw new InvalidSessionStateException($"{@event.GetType().Name} is not supported");
            }
        }

        session.PullEvents();

        return session;
    }

    public void Refresh(RefreshTokenHash refreshTokenHash, DateTime expiresAt, DateTime now)
    {
        if (IsRevoked)
        {
            throw new SessionRevokedException("The session is revoked");
        }

        if (IsExpired(now))
        {
            throw new SessionExpiredException("The session is expired");
        }

        RefreshTokenHash = refreshTokenHash;
        ExpiresAt = expiresAt;

        var @event = new SessionRefreshedEvent
        {
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = expiresAt,
            OccurredAt = now
        };
        AddEvent(@event);
    }

    public void Revoke(DateTime now)
    {
        if (IsRevoked)
        {
            throw new SessionRevokedException("The session is revoked");
        }

        RevokedAt = now;

        var @event = new SessionRevokedEvent
        {
            OccurredAt = now
        };
        AddEvent(@event);
    }

    public bool IsExpired(DateTime now)
    {
        return ExpiresAt < now;
    }

    public bool IsActive(DateTime now)
    {
        return !IsRevoked && !IsExpired(now);
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
