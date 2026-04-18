using Application.Exception;
using Application.Service.AuthTokenCodec;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Infrastructure.Service.AuthTokenCodec;

public class JwtAuthTokenCodec : IAuthTokenCodec
{
    private readonly JsonWebTokenHandler _handler = new();

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;
    private readonly SigningCredentials _credentials;

    private const int MinSecretLength = 16;

    public JwtAuthTokenCodec(IOptions<JwtAuthTokenCodecOptions> options)
    {
        _issuer = options.Value.Issuer;
        _audience = options.Value.Audience;

        if (options.Value.Secret.Length < MinSecretLength)
            throw new InternalSystemMisconfiguredException(
                $"JwtAuthTokenCodec secret must be at least {MinSecretLength} characters");

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret));
        _credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
    }

    public Task<string> EncodeAccessTokenAsync(AccessTokenPayload payload)
    {
        var descriptor = CreateDescriptor(
            payload.AccountUid,
            payload.SessionUid,
            payload.Nickname,
            payload.ExpiresAt,
            "access"
        );

        return Task.FromResult(_handler.CreateToken(descriptor));
    }

    public Task<string> EncodeRefreshTokenAsync(RefreshTokenPayload payload)
    {
        var descriptor = CreateDescriptor(
            payload.AccountUid,
            payload.SessionUid,
            payload.Nickname,
            payload.ExpiresAt,
            "refresh"
        );

        return Task.FromResult(_handler.CreateToken(descriptor));
    }

    private SecurityTokenDescriptor CreateDescriptor(
        Guid accountUid,
        Guid sessionUid,
        string nickname,
        DateTime expiresAt,
        string type)
    {
        return new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            Expires = expiresAt,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = accountUid.ToString(),
                ["sid"] = sessionUid.ToString(),
                ["nickname"] = nickname,
                ["typ"] = type
            }
        };
    }

    public async Task<AccessTokenPayload> DecodeAccessTokenAsync(string token)
    {
        var jwt = await Validate(token);

        EnsureType(jwt, "access");

        return new AccessTokenPayload
        {
            AccountUid = GetGuid(jwt, "sub"),
            SessionUid = GetGuid(jwt, "sid"),
            Nickname = GetString(jwt, "nickname"),
            ExpiresAt = jwt.ValidTo
        };
    }

    public async Task<RefreshTokenPayload> DecodeRefreshTokenAsync(string token)
    {
        var jwt = await Validate(token);

        EnsureType(jwt, "refresh");

        return new RefreshTokenPayload
        {
            AccountUid = GetGuid(jwt, "sub"),
            SessionUid = GetGuid(jwt, "sid"),
            Nickname = GetString(jwt, "nickname"),
            ExpiresAt = jwt.ValidTo
        };
    }

    private async Task<JsonWebToken> Validate(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,

            ValidateAudience = true,
            ValidAudience = _audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var result = await _handler.ValidateTokenAsync(token, parameters);

        if (!result.IsValid)
        {
            if (result.Exception is SecurityTokenExpiredException)
                throw new WrongAuthTokenException("The token is expired", result.Exception);

            throw new WrongAuthTokenException(result.Exception?.Message!, result.Exception);
        }

        return (JsonWebToken)result.SecurityToken!;
    }

    private static void EnsureType(JsonWebToken token, string expected)
    {
        var actual = token.GetClaim("typ");

        if (actual.Value != expected)
            throw new WrongAuthTokenException($"Invalid token type: expected {expected}");
    }

    private static Guid GetGuid(JsonWebToken token, string claim)
    {
        var value = token.GetClaim(claim).Value
            ?? throw new WrongAuthTokenException($"Missing claim: {claim}");

        if (!Guid.TryParse(value, out var guid))
            throw new WrongAuthTokenException($"Invalid claim: {claim}");

        return guid;
    }

    private static string GetString(JsonWebToken token, string claim)
    {
        return token.GetClaim(claim).Value
            ?? throw new WrongAuthTokenException($"Missing claim: {claim}");
    }
}

public class JwtAuthTokenCodecOptions
{
    public const string SectionName = "JwtAuthTokenCodec";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string Secret { get; init; }
}
