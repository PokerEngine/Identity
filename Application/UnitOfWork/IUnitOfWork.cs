using Domain.Entity;

namespace Application.UnitOfWork;

public interface IUnitOfWork
{
    void Register(Identity identity);
    Task CommitAsync();
}
