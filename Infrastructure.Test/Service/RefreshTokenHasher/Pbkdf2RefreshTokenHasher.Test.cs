using Application.Exception;
using Application.Service.RefreshTokenHasher;
using Infrastructure.Service.RefreshTokenHasher;

namespace Infrastructure.Test.Service.RefreshTokenHasher;

public class Pbkdf2RefreshTokenHasherTest
{
    [Fact]
    public async Task HashAsync_WhenCalled_ShouldReturnNonEmptyHash()
    {
        // Arrange
        var hasher = CreateHasher();
        var token = "some-refresh-token-value";

        // Act
        var hash = await hasher.HashAsync(token);

        // Assert
        Assert.NotEmpty((string)hash);
    }

    [Fact]
    public async Task HashAsync_WhenSameTokenHashedTwice_ShouldReturnDifferentHashes()
    {
        // Arrange
        var hasher = CreateHasher();
        var token = "some-refresh-token-value";

        // Act
        var hash1 = await hasher.HashAsync(token);
        var hash2 = await hasher.HashAsync(token);

        // Assert
        Assert.NotEqual((string)hash1, (string)hash2);
    }

    [Fact]
    public async Task VerifyAsync_WhenCorrectToken_ShouldNotThrow()
    {
        // Arrange
        var hasher = CreateHasher();
        var token = "some-refresh-token-value";
        var hash = await hasher.HashAsync(token);

        // Act & Assert
        await hasher.VerifyAsync(token, hash);
    }

    [Fact]
    public async Task VerifyAsync_WhenWrongToken_ShouldThrowException()
    {
        // Arrange
        var hasher = CreateHasher();
        var token = "some-refresh-token-value";
        var hash = await hasher.HashAsync(token);

        // Act & Assert
        var exc = await Assert.ThrowsAsync<WrongAuthTokenException>(
            async () => await hasher.VerifyAsync("wrong-token-value", hash)
        );
        Assert.Equal("The token is wrong", exc.Message);
    }

    private static IRefreshTokenHasher CreateHasher() => new Pbkdf2RefreshTokenHasher();
}
