using Application.Exception;
using Application.Repository;
using Application.Service.AuthTokenCodec;
using Application.Service.RefreshTokenHasher;
using Application.Storage;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.Command;

public record RefreshSessionCommand : ICommand
{
    public required string RefreshToken { get; init; }
}

public record RefreshSessionResponse : ICommandResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
}

public class RefreshSessionHandler(
    ISessionRepository sessionRepository,
    IAccountStorage accountStorage,
    IUnitOfWork unitOfWork,
    IRefreshTokenHasher refreshTokenHasher,
    IAuthTokenCodec authTokenCodec
) : ICommandHandler<RefreshSessionCommand, RefreshSessionResponse>
{
    public async Task<RefreshSessionResponse> HandleAsync(RefreshSessionCommand command)
    {
        var refreshTokenPayload = await authTokenCodec.DecodeRefreshTokenAsync(command.RefreshToken);

        if (refreshTokenPayload.ExpiresAt < DateTime.UtcNow)
        {
            throw new WrongAuthTokenException("The token is expired");
        }

        Session session;

        try
        {
            var events = await sessionRepository.GetEventsAsync(refreshTokenPayload.SessionUid);
            session = Session.FromEvents(refreshTokenPayload.SessionUid, events);
        }
        catch (SessionNotFoundException e)
        {
            throw new WrongAuthTokenException("The token is wrong", e);
        }

        var account = await accountStorage.GetViewAsync(session.AccountUid);

        var newAccessTokenPayload = new AccessTokenPayload
        {
            SessionUid = session.Uid,
            AccountUid = session.AccountUid,
            Nickname = account.Nickname,
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 15, 0)
        };
        var newRefreshTokenPayload = new RefreshTokenPayload
        {
            SessionUid = session.Uid,
            AccountUid = session.AccountUid,
            Nickname = account.Nickname,
            ExpiresAt = DateTime.UtcNow + new TimeSpan(30, 0, 0, 0)
        };
        var newAccessToken = await authTokenCodec.EncodeAccessTokenAsync(newAccessTokenPayload);
        var newRefreshToken = await authTokenCodec.EncodeRefreshTokenAsync(newRefreshTokenPayload);
        var newRefreshTokenHash = await refreshTokenHasher.HashAsync(newRefreshToken);

        session.Refresh(newRefreshTokenHash, newAccessTokenPayload.ExpiresAt, DateTime.UtcNow);

        unitOfWork.RegisterSession(session);
        await unitOfWork.CommitAsync();

        return new RefreshSessionResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = newAccessTokenPayload.ExpiresAt,
            RefreshTokenExpiresAt = newRefreshTokenPayload.ExpiresAt
        };
    }
}
