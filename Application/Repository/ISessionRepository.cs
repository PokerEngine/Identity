using Domain.Event;
using Domain.ValueObject;

namespace Application.Repository;

public interface ISessionRepository
{
    Task<SessionUid> GetNextUidAsync();
    Task<List<IEvent>> GetEventsAsync(SessionUid uid);
    Task AddEventsAsync(SessionUid uid, List<IEvent> events);
}
