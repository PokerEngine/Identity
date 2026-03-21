using Application.Exception;
using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using Infrastructure.Repository;
using Infrastructure.Test.Client.MongoDb;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Repository;

[Trait("Category", "Integration")]
public class MongoDbIdentityRepositoryTest(MongoDbClientFixture fixture) : IClassFixture<MongoDbClientFixture>
{
    [Fact]
    public async Task GetEventsAsync_WhenExists_ShouldReturn()
    {
        // Arrange
        var repository = CreateRepository();

        var accountUid = new AccountUid(Guid.NewGuid());
        var @event = new TestIdentityEvent
        {
            PasswordHash = new PasswordHash("abcdef"),
            OccurredAt = GetNow()
        };
        await repository.AddEventsAsync(accountUid, [@event]);

        // Act
        var events = await repository.GetEventsAsync(accountUid);

        // Assert
        Assert.Single(events);
        var typedEvent = Assert.IsType<TestIdentityEvent>(events[0]);
        Assert.Equal(@event, typedEvent);
    }

    [Fact]
    public async Task GetEventsAsync_WhenNotExists_ShouldThrowException()
    {
        // Arrange
        var repository = CreateRepository();

        var accountUid = new AccountUid(Guid.NewGuid());
        var @event = new TestIdentityEvent
        {
            PasswordHash = new PasswordHash("abcdef"),
            OccurredAt = GetNow()
        };
        await repository.AddEventsAsync(accountUid, [@event]);

        // Act & Assert
        var exc = await Assert.ThrowsAsync<IdentityNotFoundException>(
            async () => await repository.GetEventsAsync(new AccountUid(Guid.NewGuid()))
        );
        Assert.Equal("The identity is not found", exc.Message);
    }

    private IIdentityRepository CreateRepository()
    {
        var client = fixture.CreateClient();
        var options = CreateOptions();
        return new MongoDbIdentityRepository(client, options);
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
        var now = DateTime.UtcNow;
        return new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMillisecond), now.Kind);
    }
}

internal sealed record TestIdentityEvent : IEvent
{
    public required PasswordHash PasswordHash { get; init; }
    public required DateTime OccurredAt { get; init; }
}
