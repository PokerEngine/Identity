using Application.Exception;
using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using Infrastructure.Repository;
using Infrastructure.Test.Client.MongoDb;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Repository;

[Trait("Category", "Integration")]
public class MongoDbRepositoryTest(MongoDbClientFixture fixture) : IClassFixture<MongoDbClientFixture>
{
    [Fact]
    public async Task GetEventsAsync_WhenExists_ShouldReturn()
    {
        // Arrange
        var repository = CreateRepository();

        var accountUid = new AccountUid(Guid.NewGuid());
        var @event = new TestEvent
        {
            EncryptedPassword = new EncryptedPassword("abcdef"),
            OccurredAt = GetNow()
        };
        await repository.AddEventsAsync(accountUid, [@event]);

        // Act
        var events = await repository.GetEventsAsync(accountUid);

        // Assert
        Assert.Single(events);
        var typedEvent = Assert.IsType<TestEvent>(events[0]);
        Assert.Equal(@event, typedEvent);
    }

    [Fact]
    public async Task GetEventsAsync_WhenNotExists_ShouldThrowException()
    {
        // Arrange
        var repository = CreateRepository();

        var accountUid = new AccountUid(Guid.NewGuid());
        var @event = new TestEvent
        {
            EncryptedPassword = new EncryptedPassword("abcdef"),
            OccurredAt = GetNow()
        };
        await repository.AddEventsAsync(accountUid, [@event]);

        // Act & Assert
        var exc = await Assert.ThrowsAsync<AccountNotFoundException>(
            async () => await repository.GetEventsAsync(new AccountUid(Guid.NewGuid()))
        );
        Assert.Equal("The account is not found", exc.Message);
    }

    private IRepository CreateRepository()
    {
        var client = fixture.CreateClient();
        var options = CreateOptions();
        return new MongoDbRepository(client, options);
    }

    private IOptions<MongoDbRepositoryOptions> CreateOptions()
    {
        var options = new MongoDbRepositoryOptions
        {
            Database = $"test_repository_{Guid.NewGuid()}"
        };
        return Options.Create(options);
    }

    private static DateTime GetNow()
    {
        // We truncate nanoseconds because they are not supported in Mongo
        var now = DateTime.Now;
        return new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMillisecond), now.Kind);
    }
}

internal sealed record TestEvent : IEvent
{
    public required EncryptedPassword EncryptedPassword { get; init; }
    public required DateTime OccurredAt { get; init; }
}
