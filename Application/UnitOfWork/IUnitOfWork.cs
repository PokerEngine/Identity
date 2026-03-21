using Domain.Entity;

namespace Application.UnitOfWork;

public interface IUnitOfWork
{
    void RegisterIdentity(Identity identity);
    Task CommitAsync();
}
