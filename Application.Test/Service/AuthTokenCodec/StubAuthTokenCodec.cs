using Application.Exception;
using Application.Service.AuthTokenCodec;
using System.Text.Json;

namespace Application.Test.Service.AuthTokenCodec;

public class StubAuthTokenCodec : IAuthTokenCodec
{
    public Task<string> EncodeAccessTokenAsync(AccessTokenPayload accessTokenPayload)
    {
        var accessToken = JsonSerializer.Serialize(accessTokenPayload);
        return Task.FromResult(accessToken);
    }

    public Task<string> EncodeRefreshTokenAsync(RefreshTokenPayload refreshTokenPayload)
    {
        var accessToken = JsonSerializer.Serialize(refreshTokenPayload);
        return Task.FromResult(accessToken);
    }

    public Task<AccessTokenPayload> DecodeAccessTokenAsync(string accessToken)
    {
        var accessTokenPayload = JsonSerializer.Deserialize<AccessTokenPayload>(accessToken)!;

        if (accessTokenPayload.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new WrongAuthTokenException("The token is expired");
        }

        return Task.FromResult(accessTokenPayload);
    }

    public Task<RefreshTokenPayload> DecodeRefreshTokenAsync(string refreshToken)
    {
        var refreshTokenPayload = JsonSerializer.Deserialize<RefreshTokenPayload>(refreshToken)!;

        if (refreshTokenPayload.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new WrongAuthTokenException("The token is expired");
        }

        return Task.FromResult(refreshTokenPayload);
    }
}
