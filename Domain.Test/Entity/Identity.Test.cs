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
            encryptedPassword: new EncryptedPassword("abcdef")
        );

        // Assert
        Assert.Equal(accountUid, identity.AccountUid);
        Assert.Equal(new EncryptedPassword("abcdef"), identity.EncryptedPassword);

        var pulledEvents = identity.PullEvents();
        Assert.Single(pulledEvents);
        var @event = Assert.IsType<PasswordInitializedEvent>(pulledEvents[0]);
        Assert.Equal(new EncryptedPassword("abcdef"), @event.EncryptedPassword);
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
                EncryptedPassword = new EncryptedPassword("abcdef"),
                OccurredAt = new DateTime(2025, 1, 1)
            },
            new PasswordChangedEvent
            {
                EncryptedPassword = new EncryptedPassword("ghijkl"),
                OccurredAt = new DateTime(2025, 1, 1)
            }
        };

        // Act
        var identity = Identity.FromEvents(accountUid, events);

        // Assert
        Assert.Equal(accountUid, identity.AccountUid);
        Assert.Equal(new EncryptedPassword("ghijkl"), identity.EncryptedPassword);

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
                EncryptedPassword = new EncryptedPassword("abcdef"),
                OccurredAt = new DateTime(2025, 1, 1)
            },
            new PasswordInitializedEvent
            {
                EncryptedPassword = new EncryptedPassword("abcdef"),
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
            encryptedPassword: new EncryptedPassword("abcdef")
        );
        identity.PullEvents();

        // Act
        identity.ChangePassword(new EncryptedPassword("ghijkl"));

        // Assert
        Assert.Equal(new EncryptedPassword("ghijkl"), identity.EncryptedPassword);

        var pulledEvents = identity.PullEvents();
        Assert.Single(pulledEvents);
        var @event = Assert.IsType<PasswordChangedEvent>(pulledEvents[0]);
        Assert.Equal(new EncryptedPassword("ghijkl"), @event.EncryptedPassword);
    }
}
