using Application.Exception;
using Application.Repository;
using Application.Service.AuthTokenCodec;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.Command;

public record RevokeSessionCommand : ICommand
{
    public required string AccessToken { get; init; }
}

public record RevokeSessionResponse : ICommandResponse;

public class RevokeSessionHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IAuthTokenCodec authTokenCodec
) : ICommandHandler<RevokeSessionCommand, RevokeSessionResponse>
{
    public async Task<RevokeSessionResponse> HandleAsync(RevokeSessionCommand command)
    {
        var accessTokenPayload = await authTokenCodec.DecodeAccessTokenAsync(command.AccessToken);

        if (accessTokenPayload.ExpiresAt < DateTime.UtcNow)
        {
            throw new WrongAuthTokenException("The token is expired");
        }

        Session session;

        try
        {
            var events = await sessionRepository.GetEventsAsync(accessTokenPayload.SessionUid);
            session = Session.FromEvents(accessTokenPayload.SessionUid, events);
        }
        catch (SessionNotFoundException e)
        {
            throw new WrongAuthTokenException("The token is wrong", e);
        }

        session.Revoke(DateTime.UtcNow);

        unitOfWork.Register(session);
        await unitOfWork.CommitAsync();

        return new RevokeSessionResponse();
    }
}
