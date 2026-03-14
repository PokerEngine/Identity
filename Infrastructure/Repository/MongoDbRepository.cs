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

public class MongoDbRepository : IRepository
{
    private const string CollectionName = "events";
    private readonly IMongoCollection<EventDocument> _collection;

    public MongoDbRepository(MongoDbClient client, IOptions<MongoDbRepositoryOptions> options)
    {
        var db = client.Client.GetDatabase(options.Value.Database);
        _collection = db.GetCollection<EventDocument>(CollectionName);
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
        var documents = events.Select(e => new EventDocument
        {
            Type = MongoDbEventTypeResolver.GetName(e),
            AccountUid = accountUid,
            OccurredAt = e.OccurredAt,
            Data = e.ToBsonDocument(e.GetType())
        });

        await _collection.InsertManyAsync(documents);
    }
}

public class MongoDbRepositoryOptions
{
    public const string SectionName = "MongoDbRepository";

    public required string Database { get; init; }
}

internal sealed class EventDocument
{
    [BsonId]
    public ObjectId Id { get; init; }

    public required string Type { get; init; }
    public required AccountUid AccountUid { get; init; }
    public required DateTime OccurredAt { get; init; }
    public required BsonDocument Data { get; init; }
}
