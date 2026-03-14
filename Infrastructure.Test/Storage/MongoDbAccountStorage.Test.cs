using Application.Exception;
using Application.Storage;
using Infrastructure.Storage;
using Infrastructure.Test.Client.MongoDb;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Storage;

[Trait("Category", "Integration")]
public class MongoDbAccountStorageTest(MongoDbClientFixture fixture) : IClassFixture<MongoDbClientFixture>
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetDetailViewAsync_WhenExists_ShouldReturn(bool isEmailVerified)
    {
        // Arrange
        var storage = CreateAccountStorage();
        var view = new AccountView
        {
            AccountUid = Guid.NewGuid(),
            Nickname = "Alice",
            Email = "alice.alright@test.com",
            IsEmailVerified = isEmailVerified
        };
        await storage.SaveViewAsync(view);

        // Act
        var retrievedView = await storage.GetViewAsync(view.AccountUid);

        // Assert
        Assert.Equal(view, retrievedView);
    }

    [Fact]
    public async Task GetDetailViewAsync_WhenNotExists_ShouldThrowException()
    {
        // Arrange
        var storage = CreateAccountStorage();

        // Act
        var exc = await Assert.ThrowsAsync<AccountNotFoundException>(async () =>
            await storage.GetViewAsync(Guid.NewGuid()));

        // Assert
        Assert.Equal("The account is not found", exc.Message);
    }

    private IAccountStorage CreateAccountStorage()
    {
        var client = fixture.CreateClient();
        var options = CreateOptions();
        return new MongoDbAccountStorage(client, options);
    }

    private IOptions<MongoDbAccountStorageOptions> CreateOptions()
    {
        var options = new MongoDbAccountStorageOptions
        {
            Database = $"test_storage_{Guid.NewGuid()}"
        };
        return Options.Create(options);
    }
}
