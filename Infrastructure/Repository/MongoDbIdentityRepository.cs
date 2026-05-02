using Application.Exception;
using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using Infrastructure.Client.MongoDb;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Infrastructure.Repository;

public class MongoDbIdentityRepository : IIdentityRepository
{
    private const string CollectionName = "identity_events";
    private readonly IMongoCollection<IdentityEventDocument> _collection;

    public MongoDbIdentityRepository(MongoDbClient client, IOptions<MongoDbIdentityRepositoryOptions> options)
    {
        var db = client.Client.GetDatabase(options.Value.Database);
        _collection = db.GetCollection<IdentityEventDocument>(CollectionName);
    }

    public async Task<bool> ExistsAsync(AccountUid accountUid)
    {
        var document = await _collection
            .Find(e => e.AccountUid == accountUid)
            .FirstOrDefaultAsync();
        return document is not null;
    }

    public async Task<List<IEvent>> GetEventsAsync(AccountUid accountUid)
    {
        var documents = await _collection
            .Find(e => e.AccountUid == accountUid)
            .SortBy(e => e.Id)
            .ToListAsync();

        var events = new List<IEvent>();

        foreach (var document in documents)
        {
            var type = MongoDbEventTypeResolver.GetType(document.Type);
            var @event = (IEvent)BsonSerializer.Deserialize(document.Data, type);
            events.Add(@event);
        }

        if (events.Count == 0)
        {
            throw new IdentityNotFoundException("The identity is not found");
        }

        return events;
    }

    public async Task AddEventsAsync(AccountUid accountUid, List<IEvent> events)
    {
        var documents = events.Select(e => new IdentityEventDocument
        {
            Type = MongoDbEventTypeResolver.GetName(e),
            AccountUid = accountUid,
            OccurredAt = e.OccurredAt,
            Data = e.ToBsonDocument(e.GetType())
        });

        await _collection.InsertManyAsync(documents);
    }
}

public class MongoDbIdentityRepositoryOptions
{
    public const string SectionName = "MongoDbIdentityRepository";

    public required string Database { get; init; }
}

internal sealed class IdentityEventDocument
{
    [BsonId]
    public ObjectId Id { get; init; }

    public required string Type { get; init; }
    public required AccountUid AccountUid { get; init; }
    public required DateTime OccurredAt { get; init; }
    public required BsonDocument Data { get; init; }
}
