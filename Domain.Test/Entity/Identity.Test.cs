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
        var uid = new AccountUid(Guid.NewGuid());

        // Act
        var identity = Identity.FromScratch(
            uid: uid,
            encryptedPassword: new EncryptedPassword("abcdef")
        );

        // Assert
        Assert.Equal(uid, identity.Uid);
        Assert.Equal(new EncryptedPassword("abcdef"), identity.EncryptedPassword);

        var pulledEvents = identity.PullEvents();
        Assert.Single(pulledEvents);
        var @event = Assert.IsType<IdentityCreatedEvent>(pulledEvents[0]);
        Assert.Equal(new EncryptedPassword("abcdef"), @event.EncryptedPassword);
    }

    [Fact]
    public void FromEvents_WhenValid_ShouldConstruct()
    {
        // Arrange
        var uid = new AccountUid(Guid.NewGuid());
        var events = new List<IEvent>
        {
            new IdentityCreatedEvent
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
        var identity = Identity.FromEvents(uid, events);

        // Assert
        Assert.Equal(uid, identity.Uid);
        Assert.Equal(new EncryptedPassword("ghijkl"), identity.EncryptedPassword);

        var pulledEvents = identity.PullEvents();
        Assert.Empty(pulledEvents);
    }

    [Fact]
    public void FromEvents_WhenCreateDuplicated_ShouldThrowException()
    {
        // Arrange
        var uid = new AccountUid(Guid.NewGuid());
        var events = new List<IEvent>
        {
            new IdentityCreatedEvent
            {
                EncryptedPassword = new EncryptedPassword("abcdef"),
                OccurredAt = new DateTime(2025, 1, 1)
            },
            new IdentityCreatedEvent
            {
                EncryptedPassword = new EncryptedPassword("abcdef"),
                OccurredAt = new DateTime(2025, 1, 1)
            },
        };

        // Act
        var exc = Assert.Throws<InvalidIdentityStateException>(() => Identity.FromEvents(uid, events));

        // Assert
        Assert.Equal("IdentityCreatedEvent is not supported", exc.Message);
    }
}
