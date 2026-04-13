using Application.Exception;
using Application.Repository;
using Application.Service.AuthTokenCodec;
using Application.Service.PasswordHasher;
using Application.Service.RefreshTokenHasher;
using Application.Storage;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.Command;

public record CreateSessionCommand : ICommand
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public record CreateSessionResponse : ICommandResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
}

public class CreateSessionHandler(
    IIdentityRepository identityRepository,
    ISessionRepository sessionRepository,
    IAccountStorage accountStorage,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IRefreshTokenHasher refreshTokenHasher,
    IAuthTokenCodec authTokenCodec
) : ICommandHandler<CreateSessionCommand, CreateSessionResponse>
{
    public async Task<CreateSessionResponse> HandleAsync(CreateSessionCommand command)
    {
        AccountView account;

        try
        {
            account = await accountStorage.GetViewByEmailAsync(command.Email);
        }
        catch (AccountNotFoundException e)
        {
            throw new WrongCredentialsException("The credentials are wrong", e);
        }

        Identity identity;

        try
        {
            var events = await identityRepository.GetEventsAsync(account.AccountUid);
            identity = Identity.FromEvents(account.AccountUid, events);
        }
        catch (IdentityNotFoundException e)
        {
            throw new WrongCredentialsException("The credentials are wrong", e);
        }

        await passwordHasher.VerifyAsync(command.Password, identity.PasswordHash);

        var uid = await sessionRepository.GetNextUidAsync();
        var accessTokenPayload = new AccessTokenPayload
        {
            SessionUid = uid,
            AccountUid = account.AccountUid,
            Nickname = account.Nickname,
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 15, 0)
        };
        var refreshTokenPayload = new RefreshTokenPayload
        {
            SessionUid = uid,
            AccountUid = account.AccountUid,
            Nickname = account.Nickname,
            ExpiresAt = DateTime.UtcNow + new TimeSpan(30, 0, 0, 0)
        };
        var accessToken = await authTokenCodec.EncodeAccessTokenAsync(accessTokenPayload);
        var refreshToken = await authTokenCodec.EncodeRefreshTokenAsync(refreshTokenPayload);

        var session = Session.FromScratch(
            uid: uid,
            accountUid: account.AccountUid,
            refreshTokenHash: await refreshTokenHasher.HashAsync(refreshToken),
            expiresAt: accessTokenPayload.ExpiresAt,
            now: DateTime.UtcNow
        );

        unitOfWork.Register(session);
        await unitOfWork.CommitAsync();

        return new CreateSessionResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenPayload.ExpiresAt,
            RefreshTokenExpiresAt = refreshTokenPayload.ExpiresAt
        };
    }
}
