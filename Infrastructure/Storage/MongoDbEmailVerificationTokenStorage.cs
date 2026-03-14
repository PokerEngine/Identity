using Application.Exception;
using Application.Storage;
using Infrastructure.Client.MongoDb;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace Infrastructure.Storage;

public class MongoDbPasswordResetTokenStorage : IPasswordResetTokenStorage
{
    private const string CollectionName = "tokens_password_reset";
    private readonly IMongoCollection<PasswordResetTokenDocument> _collection;
    private readonly MongoDbPasswordResetTokenStorageOptions _options;

    public MongoDbPasswordResetTokenStorage(
        MongoDbClient client,
        IOptions<MongoDbPasswordResetTokenStorageOptions> options
    )
    {
        var db = client.Client.GetDatabase(options.Value.Database);

        _collection = db.GetCollection<PasswordResetTokenDocument>(CollectionName);
        // TODO: add unique indexes

        _options = options.Value;
    }

    public async Task<string> GenerateTokenAsync(Guid accountUid)
    {
        var token = GenerateRandomString();
        var document = new PasswordResetTokenDocument
        {
            AccountUid = accountUid,
            Token = token,
            GeneratedAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(document);

        return token;
    }

    public async Task<Guid> VerifyTokenAsync(string token)
    {
        var document = await _collection
            .Find(x => x.Token == token)
            .FirstOrDefaultAsync();

        if (document is null)
        {
            throw new WrongPasswordResetTokenException("The token is not found");
        }

        if (document.GeneratedAt + _options.Ttl < DateTime.UtcNow)
        {
            throw new WrongPasswordResetTokenException("The token is expired");
        }

        return document.AccountUid;
    }

    public async Task DeleteTokensAsync(Guid accountUid)
    {
        await _collection.DeleteManyAsync(x => x.AccountUid == accountUid);
    }

    private string GenerateRandomString()
    {
        var bytes = RandomNumberGenerator.GetBytes(_options.Length / 2);
        return Convert.ToHexStringLower(bytes);
    }
}

public class MongoDbPasswordResetTokenStorageOptions
{
    public const string SectionName = "MongoDbPasswordResetTokenStorage";

    public required string Database { get; init; }
    public int Length { get; init; } = 32;
    public TimeSpan Ttl { get; init; } = new(24, 0, 0);
}

internal record PasswordResetTokenDocument
{
    [BsonId]
    public ObjectId Id { get; init; }
    public required Guid AccountUid { get; init; }
    public required string Token { get; init; }
    public required DateTime GeneratedAt { get; init; }
}
