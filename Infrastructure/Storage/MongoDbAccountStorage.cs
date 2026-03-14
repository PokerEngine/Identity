using Application.Exception;
using Application.Storage;
using Infrastructure.Client.MongoDb;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Infrastructure.Storage;

public class MongoDbAccountStorage : IAccountStorage
{
    private const string AccountViewCollectionName = "views_account";
    private readonly IMongoCollection<AccountViewDocument> _accountViewCollection;

    public MongoDbAccountStorage(MongoDbClient client, IOptions<MongoDbAccountStorageOptions> options)
    {
        var db = client.Client.GetDatabase(options.Value.Database);

        _accountViewCollection = db.GetCollection<AccountViewDocument>(AccountViewCollectionName);
    }

    public async Task<bool> ExistsAsync(Guid accountUid)
    {
        var document = await _accountViewCollection
            .Find(e => e.AccountUid == accountUid)
            .FirstOrDefaultAsync();
        return document is not null;
    }

    public async Task<AccountView> GetViewAsync(Guid accountUid)
    {
        var document = await _accountViewCollection
            .Find(x => x.AccountUid == accountUid)
            .FirstOrDefaultAsync();

        if (document is null)
        {
            throw new AccountNotFoundException("The account is not found");
        }

        return new AccountView
        {
            AccountUid = document.AccountUid,
            Nickname = document.Nickname,
            Email = document.Email,
            IsEmailVerified = document.IsEmailVerified
        };
    }


    public async Task<AccountView> GetViewByEmailAsync(string email)
    {
        var document = await _accountViewCollection
            .Find(x => x.Email == email)
            .FirstOrDefaultAsync();

        if (document is null)
        {
            throw new AccountNotFoundException("The account is not found");
        }

        return new AccountView
        {
            AccountUid = document.AccountUid,
            Nickname = document.Nickname,
            Email = document.Email,
            IsEmailVerified = document.IsEmailVerified
        };
    }

    public async Task SaveViewAsync(AccountView view)
    {
        var options = new FindOneAndReplaceOptions<AccountViewDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var document = new AccountViewDocument
        {
            AccountUid = view.AccountUid,
            Nickname = view.Nickname,
            Email = view.Email,
            IsEmailVerified = view.IsEmailVerified
        };

        await _accountViewCollection.FindOneAndReplaceAsync(x => x.AccountUid == view.AccountUid, document, options);
    }
}

public class MongoDbAccountStorageOptions
{
    public const string SectionName = "MongoDbAccountStorage";

    public required string Database { get; init; }
}

public record AccountViewDocument
{
    [BsonId]
    public required Guid AccountUid { get; init; }
    public required string Nickname { get; init; }
    public required string Email { get; init; }
    public required bool IsEmailVerified { get; init; }
}
