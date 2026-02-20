using Application.Exception;
using Application.Storage;
using System.Collections.Concurrent;

namespace Application.Test.Storage;

public class StubAccountStorage : IAccountStorage
{
    private readonly ConcurrentDictionary<Guid, AccountView> _mapping = new();

    public Task<AccountView> GetViewAsync(Guid accountUid)
    {
        if (!_mapping.TryGetValue(accountUid, out var view))
        {
            throw new AccountNotFoundException("The account is not found");
        }

        return Task.FromResult(view);
    }

    public Task<bool> AccountExistsAsync(Guid accountUid)
    {
        return Task.FromResult(_mapping.ContainsKey(accountUid));
    }

    public Task SaveViewAsync(AccountView accountView)
    {
        _mapping.AddOrUpdate(accountView.Uid, accountView, (_, _) => accountView);
        return Task.CompletedTask;
    }
}
