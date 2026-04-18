using Application.Exception;
using Application.Service.PasswordHasher;
using Domain.ValueObject;
using Infrastructure.Service.PasswordHasher;

namespace Infrastructure.Test.Service.PasswordHasher;

public class Pbkdf2PasswordHasherTest
{
    [Fact]
    public async Task HashAsync_WhenCalled_ShouldReturnNonEmptyHash()
    {
        // Arrange
        var hasher = CreateHasher();
        var password = new Password("correct-horse-battery-staple");

        // Act
        var hash = await hasher.HashAsync(password);

        // Assert
        Assert.NotEmpty((string)hash);
    }

    [Fact]
    public async Task HashAsync_WhenSamePasswordHashedTwice_ShouldReturnDifferentHashes()
    {
        // Arrange
        var hasher = CreateHasher();
        var password = new Password("correct-horse-battery-staple");

        // Act
        var hash1 = await hasher.HashAsync(password);
        var hash2 = await hasher.HashAsync(password);

        // Assert
        Assert.NotEqual((string)hash1, (string)hash2);
    }

    [Fact]
    public async Task VerifyAsync_WhenCorrectPassword_ShouldNotThrow()
    {
        // Arrange
        var hasher = CreateHasher();
        var password = new Password("correct-horse-battery-staple");
        var hash = await hasher.HashAsync(password);

        // Act & Assert
        await hasher.VerifyAsync(password, hash);
    }

    [Fact]
    public async Task VerifyAsync_WhenWrongPassword_ShouldThrowException()
    {
        // Arrange
        var hasher = CreateHasher();
        var password = new Password("correct-horse-battery-staple");
        var hash = await hasher.HashAsync(password);
        var wrongPassword = new Password("wrong-password");

        // Act & Assert
        var exc = await Assert.ThrowsAsync<WrongCredentialsException>(
            async () => await hasher.VerifyAsync(wrongPassword, hash)
        );
        Assert.Equal("The credentials are wrong", exc.Message);
    }

    private static IPasswordHasher CreateHasher() => new Pbkdf2PasswordHasher();
}
