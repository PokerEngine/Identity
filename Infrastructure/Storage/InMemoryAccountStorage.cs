using Application.Exception;
using Application.Storage;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Infrastructure.Storage;

public class InMemoryAccountStorage : IAccountStorage
{
    private readonly ConcurrentDictionary<AccountUid, AccountView> _mapping = new();

    public Task<bool> ExistsAsync(Guid accountUid)
    {
        return Task.FromResult(_mapping.ContainsKey(accountUid));
    }

    public Task<AccountView> GetViewAsync(Guid accountUid)
    {
        if (!_mapping.TryGetValue(accountUid, out var view))
        {
            throw new AccountNotFoundException("The account is not found");
        }

        return Task.FromResult(view);
    }

    public Task<AccountView> GetViewByEmailAsync(string email)
    {
        foreach (var view in _mapping.Values)
        {
            if (view.Email == email)
            {
                return Task.FromResult(view);
            }
        }

        throw new AccountNotFoundException("The account is not found");
    }

    public Task SaveViewAsync(AccountView view)
    {
        _mapping[view.AccountUid] = view;
        return Task.CompletedTask;
    }
}
