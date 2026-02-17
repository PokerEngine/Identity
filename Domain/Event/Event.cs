using Domain.ValueObject;

namespace Domain.Event;

public interface IEvent
{
    DateTime OccurredAt { init; get; }
}

public sealed record IdentityCreatedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required EncryptedPassword EncryptedPassword { get; init; }
}

public sealed record PasswordChangedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required EncryptedPassword EncryptedPassword { get; init; }
}
