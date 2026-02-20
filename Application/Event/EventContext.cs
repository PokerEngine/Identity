using Domain.ValueObject;

namespace Application.Event;

public record EventContext
{
    public required AccountUid AccountUid { get; init; }
}
