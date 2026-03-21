using Domain.ValueObject;

namespace Domain.Event;

public sealed record SessionCreatedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required AccountUid AccountUid { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

public sealed record SessionRefreshedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required DateTime ExpiresAt { get; init; }
}

public sealed record SessionRevokedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }
}
