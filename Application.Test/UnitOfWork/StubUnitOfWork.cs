using Application.Test.Event;
using Application.Test.Repository;

namespace Application.Test.UnitOfWork;

public class StubUnitOfWork(
    StubIdentityRepository identityRepository,
    StubSessionRepository sessionRepository,
    StubEventDispatcher eventDispatcher
) : Application.UnitOfWork.UnitOfWork(identityRepository, sessionRepository, eventDispatcher)
{
    public readonly StubIdentityRepository IdentityRepository = identityRepository;
    public readonly StubSessionRepository SessionRepository = sessionRepository;
    public readonly StubEventDispatcher EventDispatcher = eventDispatcher;
}
