using Domain.ValueObject;

namespace Domain.Test.ValueObject;

public class RefreshTokenHashTest
{
    [Fact]
    public void Constructor_WhenValid_ShouldConstruct()
    {
        // Arrange & Act
        var refreshTokenHash = new RefreshTokenHash("abcdef");

        // Assert
        Assert.Equal("abcdef", (string)refreshTokenHash);
    }

    [Fact]
    public void ToString_WhenValid_ShouldReturnValidString()
    {
        // Arrange
        var refreshTokenHash = new RefreshTokenHash("abcdef");

        // Act & Assert
        Assert.Equal("abcdef", $"{refreshTokenHash}");
    }
}
