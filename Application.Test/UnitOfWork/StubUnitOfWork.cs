using Application.Test.Event;
using Application.Test.Repository;

namespace Application.Test.UnitOfWork;

public class StubUnitOfWork(
    StubIdentityRepository identityRepository,
    StubEventDispatcher eventDispatcher
) : Application.UnitOfWork.UnitOfWork(identityRepository, eventDispatcher)
{
    public readonly StubIdentityRepository IdentityRepository = identityRepository;
    public readonly StubEventDispatcher EventDispatcher = eventDispatcher;
}
