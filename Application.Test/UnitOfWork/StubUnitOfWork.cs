using Application.Test.Event;
using Application.Test.Repository;

namespace Application.Test.UnitOfWork;

public class StubUnitOfWork(
    StubRepository repository,
    StubEventDispatcher eventDispatcher
) : Application.UnitOfWork.UnitOfWork(repository, eventDispatcher)
{
    public readonly StubRepository Repository = repository;
    public readonly StubEventDispatcher EventDispatcher = eventDispatcher;
}
