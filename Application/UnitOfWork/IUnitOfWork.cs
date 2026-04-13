using Domain.Entity;

namespace Application.UnitOfWork;

public interface IUnitOfWork
{
    void Register(Identity identity);
    void Register(Session session);
    Task CommitAsync();
}
