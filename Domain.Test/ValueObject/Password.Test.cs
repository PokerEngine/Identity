using Domain.Exception;
using Domain.ValueObject;

namespace Domain.Test.ValueObject;

public class PasswordTest
{
    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("P@$sw0rd")]
    public void Constructor_WhenValid_ShouldConstruct(string name)
    {
        // Arrange & Act
        var password = new Password(name);

        // Assert
        Assert.Equal(name, (string)password);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("p@ss")]
    [InlineData("p@$sw0  ")]
    [InlineData("    p@ss")]
    public void Constructor_WhenTooShort_ShouldThrowException(string name)
    {
        // Arrange & Act & Assert
        var exc = Assert.Throws<InvalidPasswordException>(() => new Password(name));
        Assert.StartsWith("Password must contain at least 8 symbol(s)", exc.Message);
    }

    [Fact]
    public void Constructor_WhenTooLong_ShouldThrowException()
    {
        // Arrange & Act & Assert
        var exc = Assert.Throws<InvalidPasswordException>(() => new Password("abcdefghijklmn0pqrtsuvwxyz0123456789!"));
        Assert.StartsWith("Password must not contain more than 32 symbol(s)", exc.Message);
    }

    [Fact]
    public void ToString_WhenValid_ShouldReturnValidString()
    {
        // Arrange
        var password = new Password("P@$$w0rd");

        // Act & Assert
        Assert.Equal("********", $"{password}");
    }
}
