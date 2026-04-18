using Application.Exception;
using Application.Service.AuthTokenCodec;
using Infrastructure.Service.AuthTokenCodec;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Service.AuthTokenCodec;

public class JwtAuthTokenCodecTest
{
    private const string Secret = "test-secret-key-that-is-long-enough-for-hs256";

    [Fact]
    public async Task EncodeAccessTokenAsync_WhenCalled_ShouldReturnNonEmptyToken()
    {
        // Arrange
        var codec = CreateCodec();
        var payload = CreateAccessTokenPayload();

        // Act
        var token = await codec.EncodeAccessTokenAsync(payload);

        // Assert
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task DecodeAccessTokenAsync_WhenValidToken_ShouldReturnPayload()
    {
        // Arrange
        var codec = CreateCodec();
        var payload = CreateAccessTokenPayload();
        var token = await codec.EncodeAccessTokenAsync(payload);

        // Act
        var decoded = await codec.DecodeAccessTokenAsync(token);

        // Assert
        Assert.Equal(payload.AccountUid, decoded.AccountUid);
        Assert.Equal(payload.SessionUid, decoded.SessionUid);
        Assert.Equal(payload.Nickname, decoded.Nickname);
    }

    [Fact]
    public async Task DecodeAccessTokenAsync_WhenExpiredToken_ShouldThrowException()
    {
        // Arrange
        var codec = CreateCodec();
        var payload = CreateAccessTokenPayload(expiresAt: DateTime.UtcNow.AddSeconds(-1));
        var token = await codec.EncodeAccessTokenAsync(payload);

        // Act & Assert
        await Assert.ThrowsAsync<WrongAuthTokenException>(
            async () => await codec.DecodeAccessTokenAsync(token)
        );
    }

    [Fact]
    public async Task DecodeAccessTokenAsync_WhenRefreshToken_ShouldThrowException()
    {
        // Arrange
        var codec = CreateCodec();
        var payload = CreateRefreshTokenPayload();
        var token = await codec.EncodeRefreshTokenAsync(payload);

        // Act & Assert
        await Assert.ThrowsAsync<WrongAuthTokenException>(
            async () => await codec.DecodeAccessTokenAsync(token)
        );
    }

    [Fact]
    public async Task EncodeRefreshTokenAsync_WhenCalled_ShouldReturnNonEmptyToken()
    {
        // Arrange
        var codec = CreateCodec();
        var payload = CreateRefreshTokenPayload();

        // Act
        var token = await codec.EncodeRefreshTokenAsync(payload);

        // Assert
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task DecodeRefreshTokenAsync_WhenValidToken_ShouldReturnPayload()
    {
        // Arrange
        var codec = CreateCodec();
        var payload = CreateRefreshTokenPayload();
        var token = await codec.EncodeRefreshTokenAsync(payload);

        // Act
        var decoded = await codec.DecodeRefreshTokenAsync(token);

        // Assert
        Assert.Equal(payload.AccountUid, decoded.AccountUid);
        Assert.Equal(payload.SessionUid, decoded.SessionUid);
        Assert.Equal(payload.Nickname, decoded.Nickname);
    }

    [Fact]
    public async Task DecodeRefreshTokenAsync_WhenExpiredToken_ShouldThrowException()
    {
        // Arrange
        var codec = CreateCodec();
        var payload = CreateRefreshTokenPayload(expiresAt: DateTime.UtcNow.AddSeconds(-1));
        var token = await codec.EncodeRefreshTokenAsync(payload);

        // Act & Assert
        await Assert.ThrowsAsync<WrongAuthTokenException>(
            async () => await codec.DecodeRefreshTokenAsync(token)
        );
    }

    [Fact]
    public async Task DecodeRefreshTokenAsync_WhenAccessToken_ShouldThrowException()
    {
        // Arrange
        var codec = CreateCodec();
        var payload = CreateAccessTokenPayload();
        var token = await codec.EncodeAccessTokenAsync(payload);

        // Act & Assert
        await Assert.ThrowsAsync<WrongAuthTokenException>(
            async () => await codec.DecodeRefreshTokenAsync(token)
        );
    }

    private IAuthTokenCodec CreateCodec()
    {
        var options = Options.Create(new JwtAuthTokenCodecOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            Secret = Secret
        });
        return new JwtAuthTokenCodec(options);
    }

    private static AccessTokenPayload CreateAccessTokenPayload(DateTime? expiresAt = null)
    {
        return new AccessTokenPayload
        {
            AccountUid = Guid.NewGuid(),
            SessionUid = Guid.NewGuid(),
            Nickname = "Alice",
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1)
        };
    }

    private static RefreshTokenPayload CreateRefreshTokenPayload(DateTime? expiresAt = null)
    {
        return new RefreshTokenPayload
        {
            AccountUid = Guid.NewGuid(),
            SessionUid = Guid.NewGuid(),
            Nickname = "Alice",
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7)
        };
    }
}
