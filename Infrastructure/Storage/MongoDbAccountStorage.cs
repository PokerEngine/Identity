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
            .Find(e => e.Uid == accountUid)
            .FirstOrDefaultAsync();
        return document is not null;
    }

    public async Task<AccountView> GetViewAsync(Guid accountUid)
    {
        var document = await _accountViewCollection
            .Find(x => x.Uid == accountUid)
            .FirstOrDefaultAsync();

        if (document is null)
        {
            throw new AccountNotFoundException("The account is not found");
        }

        return new AccountView
        {
            Uid = document.Uid,
            Nickname = document.Nickname,
            Email = document.Email
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
            Uid = document.Uid,
            Nickname = document.Nickname,
            Email = document.Email
        };
    }

    public async Task SaveViewAsync(AccountView accountView)
    {
        var options = new FindOneAndReplaceOptions<AccountViewDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var document = new AccountViewDocument
        {
            Uid = accountView.Uid,
            Nickname = accountView.Nickname,
            Email = accountView.Email
        };

        await _accountViewCollection.FindOneAndReplaceAsync(x => x.Uid == accountView.Uid, document, options);
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
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
    public required string Email { get; init; }
}
