namespace Domain.Event;

public interface IEvent
{
    DateTime OccurredAt { init; get; }
}
