namespace Infrastructure.Repository;

public class MongoDbRepositoryOptions
{
    public const string SectionName = "MongoDbIdentityRepository";

    public required string Database { get; init; }
}
