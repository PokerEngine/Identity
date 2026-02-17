using Domain.ValueObject;

namespace Domain.Test.ValueObject;

public class EncryptedPasswordTest
{
    [Fact]
    public void Constructor_WhenValid_ShouldConstruct()
    {
        // Arrange & Act
        var encryptedPassword = new EncryptedPassword("abcdef");

        // Assert
        Assert.Equal("abcdef", (string)encryptedPassword);
    }

    [Fact]
    public void ToString_WhenValid_ShouldReturnValidString()
    {
        // Arrange
        var encryptedPassword = new EncryptedPassword("abcdef");

        // Act & Assert
        Assert.Equal("********", $"{encryptedPassword}");
    }
}
