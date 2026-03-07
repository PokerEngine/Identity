using Application.Exception;
using Application.Storage;
using Infrastructure.Storage;
using Infrastructure.Test.Client.MongoDb;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Storage;

[Trait("Category", "Integration")]
public class MongoDbAccountStorageTest(MongoDbClientFixture fixture) : IClassFixture<MongoDbClientFixture>
{
    [Fact]
    public async Task GetDetailViewAsync_WhenExists_ShouldReturn()
    {
        // Arrange
        var storage = CreateAccountStorage();
        var accountView = new AccountView { Uid = Guid.NewGuid(), Nickname = "Test", Email = "test@test.com" };
        await storage.SaveViewAsync(accountView);

        // Act
        var view = await storage.GetViewAsync(accountView.Uid);

        // Assert
        Assert.Equal("Test", view.Nickname);
        Assert.Equal("test@test.com", view.Email);
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

    private IOptions<MongoDbStorageOptions> CreateOptions()
    {
        var options = new MongoDbStorageOptions
        {
            Database = $"test_storage_{Guid.NewGuid()}"
        };
        return Options.Create(options);
    }
}
