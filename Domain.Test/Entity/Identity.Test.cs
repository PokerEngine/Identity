using Domain.Entity;
using Domain.Event;
using Domain.Exception;
using Domain.ValueObject;

namespace Domain.Test.Entity;

public class IdentityTest
{
    [Fact]
    public void FromScratch_WhenValid_ShouldConstruct()
    {
        // Arrange
        var accountUid = new AccountUid(Guid.NewGuid());

        // Act
        var identity = Identity.FromScratch(
            accountUid: accountUid,
            passwordHash: new PasswordHash("abcdef"),
            now: new DateTime(2025, 1, 1)
        );

        // Assert
        Assert.Equal(accountUid, identity.AccountUid);
        Assert.Equal(new PasswordHash("abcdef"), identity.PasswordHash);
        Assert.Equal(new DateTime(2025, 1, 1), identity.CreatedAt);

        var pulledEvents = identity.PullEvents();
        Assert.Single(pulledEvents);
        var @event = Assert.IsType<PasswordInitializedEvent>(pulledEvents[0]);
        Assert.Equal(new PasswordHash("abcdef"), @event.PasswordHash);
        Assert.Equal(new DateTime(2025, 1, 1), @event.OccurredAt);
    }

    [Fact]
    public void FromEvents_WhenValid_ShouldConstruct()
    {
        // Arrange
        var accountUid = new AccountUid(Guid.NewGuid());
        var events = new List<IEvent>
        {
            new PasswordInitializedEvent
            {
                PasswordHash = new PasswordHash("abcdef"),
                OccurredAt = new DateTime(2025, 1, 1)
            },
            new PasswordChangedEvent
            {
                PasswordHash = new PasswordHash("ghijkl"),
                OccurredAt = new DateTime(2025, 1, 2)
            }
        };

        // Act
        var identity = Identity.FromEvents(accountUid, events);

        // Assert
        Assert.Equal(accountUid, identity.AccountUid);
        Assert.Equal(new PasswordHash("ghijkl"), identity.PasswordHash);
        Assert.Equal(new DateTime(2025, 1, 1), identity.CreatedAt);

        var pulledEvents = identity.PullEvents();
        Assert.Empty(pulledEvents);
    }

    [Fact]
    public void FromEvents_WhenCreateDuplicated_ShouldThrowException()
    {
        // Arrange
        var accountUid = new AccountUid(Guid.NewGuid());
        var events = new List<IEvent>
        {
            new PasswordInitializedEvent
            {
                PasswordHash = new PasswordHash("abcdef"),
                OccurredAt = new DateTime(2025, 1, 1)
            },
            new PasswordInitializedEvent
            {
                PasswordHash = new PasswordHash("abcdef"),
                OccurredAt = new DateTime(2025, 1, 1)
            },
        };

        // Act
        var exc = Assert.Throws<InvalidIdentityStateException>(() => Identity.FromEvents(accountUid, events));

        // Assert
        Assert.Equal("PasswordInitializedEvent is not supported", exc.Message);
    }

    [Fact]
    public void ChangePassword_WhenValid_ShouldChangePassword()
    {
        // Arrange
        var accountUid = new AccountUid(Guid.NewGuid());
        var identity = Identity.FromScratch(
            accountUid: accountUid,
            passwordHash: new PasswordHash("abcdef"),
            now: new DateTime(2025, 1, 1)
        );
        identity.PullEvents();

        // Act
        identity.ChangePassword(new PasswordHash("ghijkl"), new DateTime(2025, 1, 2));

        // Assert
        Assert.Equal(new PasswordHash("ghijkl"), identity.PasswordHash);

        var pulledEvents = identity.PullEvents();
        Assert.Single(pulledEvents);
        var @event = Assert.IsType<PasswordChangedEvent>(pulledEvents[0]);
        Assert.Equal(new PasswordHash("ghijkl"), @event.PasswordHash);
        Assert.Equal(new DateTime(2025, 1, 2), @event.OccurredAt);
    }
}
