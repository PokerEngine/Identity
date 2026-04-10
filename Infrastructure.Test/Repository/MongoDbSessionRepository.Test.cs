using Application.Exception;
using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using Infrastructure.Repository;
using Infrastructure.Test.Client.MongoDb;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Repository;

[Trait("Category", "Integration")]
public class MongoDbSessionRepositoryTest(MongoDbClientFixture fixture) : IClassFixture<MongoDbClientFixture>
{
    [Fact]
    public async Task GetEventsAsync_WhenExists_ShouldReturn()
    {
        // Arrange
        var repository = CreateRepository();

        var uid = new SessionUid(Guid.NewGuid());
        var @event = new TestSessionEvent
        {
            RefreshTokenHash = new RefreshTokenHash("abcdef"),
            OccurredAt = GetNow()
        };
        await repository.AddEventsAsync(uid, [@event]);

        // Act
        var events = await repository.GetEventsAsync(uid);

        // Assert
        Assert.Single(events);
        var typedEvent = Assert.IsType<TestSessionEvent>(events[0]);
        Assert.Equal(@event, typedEvent);
    }

    [Fact]
    public async Task GetEventsAsync_WhenNotExists_ShouldThrowException()
    {
        // Arrange
        var repository = CreateRepository();

        var uid = new SessionUid(Guid.NewGuid());
        var @event = new TestSessionEvent
        {
            RefreshTokenHash = new RefreshTokenHash("abcdef"),
            OccurredAt = GetNow()
        };
        await repository.AddEventsAsync(uid, [@event]);

        // Act & Assert
        var exc = await Assert.ThrowsAsync<SessionNotFoundException>(
            async () => await repository.GetEventsAsync(new SessionUid(Guid.NewGuid()))
        );
        Assert.Equal("The session is not found", exc.Message);
    }

    private ISessionRepository CreateRepository()
    {
        var client = fixture.CreateClient();
        var options = CreateOptions();
        return new MongoDbSessionRepository(client, options);
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

internal sealed record TestSessionEvent : IEvent
{
    public required RefreshTokenHash RefreshTokenHash { get; init; }
    public required DateTime OccurredAt { get; init; }
}
