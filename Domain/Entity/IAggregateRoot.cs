using Domain.Event;

namespace Domain.Entity;

public interface IAggregateRoot
{
    List<IEvent> PullEvents();
}
