using Application.Command;
using Application.Exception;
using Application.Service.AuthTokenCodec;
using Application.Storage;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.AuthTokenCodec;
using Application.Test.Service.PasswordHasher;
using Application.Test.Service.RefreshTokenHasher;
using Application.Test.Storage;
using Application.Test.UnitOfWork;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Test.Command;

public class RevokeSessionTest
{
    [Fact]
    public async Task HandleAsync_WhenSessionExists_ShouldRevoke()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var accountStorage = new StubAccountStorage();
        var passwordHasher = new StubPasswordHasher();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var authTokenCodec = new StubAuthTokenCodec();
        var unitOfWork = CreateUnitOfWork();
        var account = new AccountView
        {
            AccountUid = accountUid,
            Nickname = "Alice",
            Email = "alice.alright@test.com",
            IsEmailVerified = true
        };
        await accountStorage.SaveViewAsync(account);
        await ConfirmPasswordResetAsync(passwordHasher, passwordResetTokenStorage, unitOfWork, accountUid);
        var accessToken = await CreateSessionAsync(authTokenCodec, accountStorage, unitOfWork);

        var command = new RevokeSessionCommand { AccessToken = accessToken };
        var handler = new RevokeSessionHandler(
            sessionRepository: unitOfWork.SessionRepository,
            unitOfWork: unitOfWork,
            authTokenCodec: authTokenCodec
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        var accessTokenPayload = await authTokenCodec.DecodeAccessTokenAsync(accessToken);
        var events = await unitOfWork.SessionRepository.GetEventsAsync(new SessionUid(accessTokenPayload.SessionUid));
        var session = Domain.Entity.Session.FromEvents(new SessionUid(accessTokenPayload.SessionUid), events);
        Assert.True(session.IsRevoked);

        var dispatchedEvents = unitOfWork.EventDispatcher.GetDispatchedEvents();
        Assert.Single(dispatchedEvents);
        var sessionRevokedEvent = Assert.IsType<SessionRevokedEvent>(dispatchedEvents[0]);
        Assert.Equal(new SessionUid(accessTokenPayload.SessionUid), sessionRevokedEvent.SessionUid);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenExpired_ShouldThrowException()
    {
        // Arrange
        var authTokenCodec = new StubAuthTokenCodec();
        var unitOfWork = CreateUnitOfWork();
        var accessTokenPayload = new AccessTokenPayload
        {
            SessionUid = Guid.NewGuid(),
            AccountUid = Guid.NewGuid(),
            Nickname = "Alice",
            ExpiresAt = DateTime.UtcNow - new TimeSpan(1, 0, 0)
        };
        var accessToken = await authTokenCodec.EncodeAccessTokenAsync(accessTokenPayload);

        var command = new RevokeSessionCommand { AccessToken = accessToken };
        var handler = new RevokeSessionHandler(
            sessionRepository: unitOfWork.SessionRepository,
            unitOfWork: unitOfWork,
            authTokenCodec: authTokenCodec
        );

        // Act
        var exc = await Assert.ThrowsAsync<WrongAuthTokenException>(async () =>
            await handler.HandleAsync(command));

        // Assert
        Assert.Equal("The token is expired", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionNotFound_ShouldThrowException()
    {
        // Arrange
        var authTokenCodec = new StubAuthTokenCodec();
        var unitOfWork = CreateUnitOfWork();
        var accessTokenPayload = new AccessTokenPayload
        {
            SessionUid = Guid.NewGuid(),
            AccountUid = Guid.NewGuid(),
            Nickname = "Alice",
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 15, 0)
        };
        var accessToken = await authTokenCodec.EncodeAccessTokenAsync(accessTokenPayload);

        var command = new RevokeSessionCommand { AccessToken = accessToken };
        var handler = new RevokeSessionHandler(
            sessionRepository: unitOfWork.SessionRepository,
            unitOfWork: unitOfWork,
            authTokenCodec: authTokenCodec
        );

        // Act
        var exc = await Assert.ThrowsAsync<WrongAuthTokenException>(async () =>
            await handler.HandleAsync(command));

        // Assert
        Assert.Equal("The token is wrong", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionAlreadyRevoked_ShouldThrowException()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var accountStorage = new StubAccountStorage();
        var passwordHasher = new StubPasswordHasher();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var authTokenCodec = new StubAuthTokenCodec();
        var unitOfWork = CreateUnitOfWork();
        var account = new AccountView
        {
            AccountUid = accountUid,
            Nickname = "Alice",
            Email = "alice.alright@test.com",
            IsEmailVerified = true
        };
        await accountStorage.SaveViewAsync(account);
        await ConfirmPasswordResetAsync(passwordHasher, passwordResetTokenStorage, unitOfWork, accountUid);
        var accessToken = await CreateSessionAsync(authTokenCodec, accountStorage, unitOfWork);

        var command = new RevokeSessionCommand { AccessToken = accessToken };
        var handler = new RevokeSessionHandler(
            sessionRepository: unitOfWork.SessionRepository,
            unitOfWork: unitOfWork,
            authTokenCodec: authTokenCodec
        );
        await handler.HandleAsync(command);
        unitOfWork.EventDispatcher.ClearDispatchedEvents();

        // Act
        var exc = await Assert.ThrowsAsync<Domain.Exception.SessionRevokedException>(async () =>
            await handler.HandleAsync(command));

        // Assert
        Assert.Equal("The session is revoked", exc.Message);
    }

    private async Task ConfirmPasswordResetAsync(
        StubPasswordHasher passwordHasher,
        StubPasswordResetTokenStorage passwordResetTokenStorage,
        StubUnitOfWork unitOfWork,
        Guid accountUid,
        string password = "P@$$w0rd"
    )
    {
        var resetToken = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);
        var command = new ConfirmPasswordResetCommand { ResetToken = resetToken, Password = password };
        var handler = new ConfirmPasswordResetHandler(
            identityRepository: unitOfWork.IdentityRepository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordHasher: passwordHasher,
            unitOfWork: unitOfWork
        );
        await handler.HandleAsync(command);
        unitOfWork.EventDispatcher.ClearDispatchedEvents();
    }

    private async Task<string> CreateSessionAsync(
        StubAuthTokenCodec authTokenCodec,
        StubAccountStorage accountStorage,
        StubUnitOfWork unitOfWork,
        string email = "alice.alright@test.com",
        string password = "P@$$w0rd"
    )
    {
        var command = new CreateSessionCommand { Email = email, Password = password };
        var handler = new CreateSessionHandler(
            identityRepository: unitOfWork.IdentityRepository,
            sessionRepository: unitOfWork.SessionRepository,
            accountStorage: accountStorage,
            unitOfWork: unitOfWork,
            passwordHasher: new StubPasswordHasher(),
            refreshTokenHasher: new StubRefreshTokenHasher(),
            authTokenCodec: authTokenCodec
        );
        var response = await handler.HandleAsync(command);
        unitOfWork.EventDispatcher.ClearDispatchedEvents();
        return response.AccessToken;
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var identityRepository = new StubIdentityRepository();
        var sessionRepository = new StubSessionRepository();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(identityRepository, sessionRepository, eventDispatcher);
    }
}
