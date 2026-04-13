using Domain.Entity;
using Domain.Event;
using Domain.Exception;
using Domain.ValueObject;

namespace Domain.Test.Entity;

public class SessionTest
{
    [Fact]
    public void FromScratch_WhenValid_ShouldConstruct()
    {
        // Arrange
        var uid = new SessionUid(Guid.NewGuid());
        var accountUid = new AccountUid(Guid.NewGuid());

        // Act
        var session = Session.FromScratch(
            uid: uid,
            accountUid: accountUid,
            refreshTokenHash: new RefreshTokenHash("abcdef"),
            expiresAt: new DateTime(2025, 1, 2),
            now: new DateTime(2025, 1, 1)
        );

        // Assert
        Assert.Equal(uid, session.Uid);
        Assert.Equal(accountUid, session.AccountUid);
        Assert.Equal(new RefreshTokenHash("abcdef"), session.RefreshTokenHash);
        Assert.Equal(new DateTime(2025, 1, 2), session.ExpiresAt);
        Assert.Null(session.RevokedAt);
        Assert.Equal(new DateTime(2025, 1, 1), session.CreatedAt);

        var pulledEvents = session.PullEvents();
        Assert.Single(pulledEvents);
        var @event = Assert.IsType<SessionCreatedEvent>(pulledEvents[0]);
        Assert.Equal(uid, @event.SessionUid);
        Assert.Equal(accountUid, @event.AccountUid);
        Assert.Equal(new RefreshTokenHash("abcdef"), @event.RefreshTokenHash);
        Assert.Equal(new DateTime(2025, 1, 2), @event.ExpiresAt);
        Assert.Equal(new DateTime(2025, 1, 1), @event.OccurredAt);
    }

    [Fact]
    public void FromEvents_WhenValid_ShouldConstruct()
    {
        // Arrange
        var uid = new SessionUid(Guid.NewGuid());
        var accountUid = new AccountUid(Guid.NewGuid());
        var events = new List<IEvent>
        {
            new SessionCreatedEvent
            {
                SessionUid = uid,
                AccountUid = accountUid,
                RefreshTokenHash = new RefreshTokenHash("abcdef"),
                ExpiresAt = new DateTime(2025, 1, 2),
                OccurredAt = new DateTime(2025, 1, 1)
            },
            new SessionRefreshedEvent
            {
                SessionUid = uid,
                RefreshTokenHash = new RefreshTokenHash("ghijkl"),
                ExpiresAt = new DateTime(2025, 1, 3),
                OccurredAt = new DateTime(2025, 1, 2)
            },
            new SessionRevokedEvent
            {
                SessionUid = uid,
                OccurredAt = new DateTime(2025, 1, 4)
            }
        };

        // Act
        var session = Session.FromEvents(uid, events);

        // Assert
        Assert.Equal(uid, session.Uid);
        Assert.Equal(accountUid, session.AccountUid);
        Assert.Equal(new RefreshTokenHash("ghijkl"), session.RefreshTokenHash);
        Assert.Equal(new DateTime(2025, 1, 3), session.ExpiresAt);
        Assert.Equal(new DateTime(2025, 1, 4), session.RevokedAt);
        Assert.Equal(new DateTime(2025, 1, 1), session.CreatedAt);

        var pulledEvents = session.PullEvents();
        Assert.Empty(pulledEvents);
    }

    [Fact]
    public void FromEvents_WhenCreateDuplicated_ShouldThrowException()
    {
        // Arrange
        var uid = new SessionUid(Guid.NewGuid());
        var accountUid = new AccountUid(Guid.NewGuid());
        var events = new List<IEvent>
        {
            new SessionCreatedEvent
            {
                SessionUid = uid,
                AccountUid = accountUid,
                RefreshTokenHash = new RefreshTokenHash("abcdef"),
                ExpiresAt = new DateTime(2025, 1, 2),
                OccurredAt = new DateTime(2025, 1, 1)
            },
            new SessionCreatedEvent
            {
                SessionUid = uid,
                AccountUid = accountUid,
                RefreshTokenHash = new RefreshTokenHash("abcdef"),
                ExpiresAt = new DateTime(2025, 1, 2),
                OccurredAt = new DateTime(2025, 1, 1)
            },
        };

        // Act
        var exc = Assert.Throws<InvalidSessionStateException>(() => Session.FromEvents(uid, events));

        // Assert
        Assert.Equal("SessionCreatedEvent is not supported", exc.Message);
    }

    [Fact]
    public void Refresh_WhenActive_ShouldRefresh()
    {
        // Arrange
        var uid = new SessionUid(Guid.NewGuid());
        var accountUid = new AccountUid(Guid.NewGuid());
        var session = Session.FromScratch(
            uid: uid,
            accountUid: accountUid,
            refreshTokenHash: new RefreshTokenHash("abcdef"),
            expiresAt: new DateTime(2025, 1, 2),
            now: new DateTime(2025, 1, 1)
        );
        session.PullEvents();

        // Act
        session.Refresh(new RefreshTokenHash("ghijkl"), new DateTime(2025, 1, 3), new DateTime(2025, 1, 2));

        // Assert
        Assert.Equal(new RefreshTokenHash("ghijkl"), session.RefreshTokenHash);
        Assert.Equal(new DateTime(2025, 1, 3), session.ExpiresAt);

        var pulledEvents = session.PullEvents();
        Assert.Single(pulledEvents);
        var @event = Assert.IsType<SessionRefreshedEvent>(pulledEvents[0]);
        Assert.Equal(uid, @event.SessionUid);
        Assert.Equal(new RefreshTokenHash("ghijkl"), @event.RefreshTokenHash);
        Assert.Equal(new DateTime(2025, 1, 3), @event.ExpiresAt);
        Assert.Equal(new DateTime(2025, 1, 2), @event.OccurredAt);
    }

    [Fact]
    public void Refresh_WhenExpired_ShouldThrowException()
    {
        // Arrange
        var uid = new SessionUid(Guid.NewGuid());
        var accountUid = new AccountUid(Guid.NewGuid());
        var session = Session.FromScratch(
            uid: uid,
            accountUid: accountUid,
            refreshTokenHash: new RefreshTokenHash("abcdef"),
            expiresAt: new DateTime(2025, 1, 2),
            now: new DateTime(2025, 1, 1)
        );
        session.PullEvents();

        // Act
        var exc = Assert.Throws<SessionExpiredException>(
            () => session.Refresh(new RefreshTokenHash("ghijkl"), new DateTime(2025, 1, 4), new DateTime(2025, 1, 3))
        );

        // Assert
        Assert.Equal("The session is expired", exc.Message);

        Assert.Empty(session.PullEvents());
    }

    [Fact]
    public void Refresh_WhenRevoked_ShouldThrowException()
    {
        // Arrange
        var uid = new SessionUid(Guid.NewGuid());
        var accountUid = new AccountUid(Guid.NewGuid());
        var session = Session.FromScratch(
            uid: uid,
            accountUid: accountUid,
            refreshTokenHash: new RefreshTokenHash("abcdef"),
            expiresAt: new DateTime(2025, 1, 2),
            now: new DateTime(2025, 1, 1)
        );
        session.Revoke(new DateTime(2025, 1, 2));
        session.PullEvents();

        // Act
        var exc = Assert.Throws<SessionRevokedException>(
            () => session.Refresh(new RefreshTokenHash("ghijkl"), new DateTime(2025, 1, 3), new DateTime(2025, 1, 2))
        );

        // Assert
        Assert.Equal("The session is revoked", exc.Message);

        Assert.Empty(session.PullEvents());
    }

    [Fact]
    public void Revoke_WhenNotRevoked_ShouldRevoke()
    {
        // Arrange
        var uid = new SessionUid(Guid.NewGuid());
        var accountUid = new AccountUid(Guid.NewGuid());
        var session = Session.FromScratch(
            uid: uid,
            accountUid: accountUid,
            refreshTokenHash: new RefreshTokenHash("abcdef"),
            expiresAt: new DateTime(2025, 1, 2),
            now: new DateTime(2025, 1, 1)
        );
        session.PullEvents();

        // Act
        session.Revoke(new DateTime(2025, 1, 2));

        // Assert
        Assert.Equal(new DateTime(2025, 1, 2), session.RevokedAt);

        var pulledEvents = session.PullEvents();
        Assert.Single(pulledEvents);
        var @event = Assert.IsType<SessionRevokedEvent>(pulledEvents[0]);
        Assert.Equal(uid, @event.SessionUid);
        Assert.Equal(new DateTime(2025, 1, 2), @event.OccurredAt);
    }

    [Fact]
    public void Revoke_WhenRevoked_ShouldThrowException()
    {
        // Arrange
        var uid = new SessionUid(Guid.NewGuid());
        var accountUid = new AccountUid(Guid.NewGuid());
        var session = Session.FromScratch(
            uid: uid,
            accountUid: accountUid,
            refreshTokenHash: new RefreshTokenHash("abcdef"),
            expiresAt: new DateTime(2025, 1, 2),
            now: new DateTime(2025, 1, 1)
        );
        session.Revoke(new DateTime(2025, 1, 1));
        session.PullEvents();

        // Act
        var exc = Assert.Throws<SessionRevokedException>(() => session.Revoke(new DateTime(2025, 1, 2)));

        // Assert
        Assert.Equal("The session is revoked", exc.Message);

        Assert.Empty(session.PullEvents());
    }
}
