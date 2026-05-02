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

public class MongoDbSessionRepository : ISessionRepository
{
    private const string CollectionName = "session_events";
    private readonly IMongoCollection<SessionEventDocument> _collection;

    public MongoDbSessionRepository(MongoDbClient client, IOptions<MongoDbSessionRepositoryOptions> options)
    {
        var db = client.Client.GetDatabase(options.Value.Database);
        _collection = db.GetCollection<SessionEventDocument>(CollectionName);
    }

    public Task<SessionUid> GetNextUidAsync()
    {
        return Task.FromResult(new SessionUid(Guid.NewGuid()));
    }

    public async Task<List<IEvent>> GetEventsAsync(SessionUid uid)
    {
        var documents = await _collection
            .Find(e => e.SessionUid == uid)
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
            throw new SessionNotFoundException("The session is not found");
        }

        return events;
    }

    public async Task AddEventsAsync(SessionUid uid, List<IEvent> events)
    {
        var documents = events.Select(e => new SessionEventDocument
        {
            Type = MongoDbEventTypeResolver.GetName(e),
            SessionUid = uid,
            OccurredAt = e.OccurredAt,
            Data = e.ToBsonDocument(e.GetType())
        });

        await _collection.InsertManyAsync(documents);
    }
}

public class MongoDbSessionRepositoryOptions
{
    public const string SectionName = "MongoDbSessionRepository";

    public required string Database { get; init; }
}

internal sealed class SessionEventDocument
{
    [BsonId]
    public ObjectId Id { get; init; }

    public required string Type { get; init; }
    public required SessionUid SessionUid { get; init; }
    public required DateTime OccurredAt { get; init; }
    public required BsonDocument Data { get; init; }
}
