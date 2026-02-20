using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Storage;

namespace Application.Test.UnitOfWork;

public class StubUnitOfWork(
    StubRepository repository,
    StubAccountStorage accountStorage,
    StubEventDispatcher eventDispatcher
) : Application.UnitOfWork.UnitOfWork(repository, eventDispatcher)
{
    public readonly StubRepository Repository = repository;
    public readonly StubAccountStorage AccountStorage = accountStorage;
    public readonly StubEventDispatcher EventDispatcher = eventDispatcher;
}
