using Domain.ValueObject;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Infrastructure.Client.MongoDb;

public class MongoDbClient
{
    public MongoClient Client;
    public MongoDbClient(IOptions<MongoDbClientOptions> options)
    {
        var url = $"mongodb://{options.Value.Username}:{options.Value.Password}@{options.Value.Host}:{options.Value.Port}";
        Client = new MongoClient(url);

        MongoDbSerializerConfig.Register();
    }
}

public class MongoDbClientOptions
{
    public const string SectionName = "MongoDb";

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}

internal static class MongoDbSerializerConfig
{
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.TryRegisterSerializer(new AccountUidSerializer());
        BsonSerializer.TryRegisterSerializer(new PasswordHashSerializer());
    }
}

internal sealed class AccountUidSerializer : SerializerBase<AccountUid>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, AccountUid value)
        => context.Writer.WriteGuid(value);

    public override AccountUid Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => context.Reader.ReadGuid();
}

internal sealed class PasswordHashSerializer : SerializerBase<PasswordHash>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PasswordHash value)
        => context.Writer.WriteString(value);

    public override PasswordHash Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => context.Reader.ReadString();
}
