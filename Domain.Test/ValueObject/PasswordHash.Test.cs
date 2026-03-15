using Domain.ValueObject;

namespace Domain.Test.ValueObject;

public class PasswordHashTest
{
    [Fact]
    public void Constructor_WhenValid_ShouldConstruct()
    {
        // Arrange & Act
        var passwordHash = new PasswordHash("abcdef");

        // Assert
        Assert.Equal("abcdef", (string)passwordHash);
    }

    [Fact]
    public void ToString_WhenValid_ShouldReturnValidString()
    {
        // Arrange
        var passwordHash = new PasswordHash("abcdef");

        // Act & Assert
        Assert.Equal("********", $"{passwordHash}");
    }
}
