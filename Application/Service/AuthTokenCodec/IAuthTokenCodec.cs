namespace Application.Service.AuthTokenCodec;

public interface IAuthTokenCodec
{
    Task<string> EncodeAccessTokenAsync(AccessTokenPayload accessTokenPayload);
    Task<string> EncodeRefreshTokenAsync(RefreshTokenPayload refreshTokenPayload);
    Task<AccessTokenPayload> DecodeAccessTokenAsync(string accessToken);
    Task<RefreshTokenPayload> DecodeRefreshTokenAsync(string refreshToken);
}

public record AuthTokenPair
{
    public required string AccessToken { init; get; }
    public required string RefreshToken { init; get; }
    public required DateTime AccessTokenExpiresAt { init; get; }
    public required DateTime RefreshTokenExpiresAt { init; get; }
}

public record AccessTokenPayload
{
    public required Guid SessionUid { init; get; }
    public required Guid AccountUid { init; get; }
    public required string Nickname { init; get; }
    public required DateTime ExpiresAt { init; get; }
}

public record RefreshTokenPayload
{
    public required Guid SessionUid { init; get; }
    public required Guid AccountUid { init; get; }
    public required string Nickname { init; get; }
    public required DateTime ExpiresAt { init; get; }
}
