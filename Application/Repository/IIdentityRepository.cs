using Domain.Event;
using Domain.ValueObject;

namespace Application.Repository;

public interface IIdentityRepository
{
    Task<bool> ExistsAsync(AccountUid accountUid);
    Task<List<IEvent>> GetEventsAsync(AccountUid accountUid);
    Task AddEventsAsync(AccountUid accountUid, List<IEvent> events);
}
