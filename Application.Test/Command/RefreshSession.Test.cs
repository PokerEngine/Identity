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

public class RefreshSessionTest
{
    [Fact]
    public async Task HandleAsync_SessionExists_ShouldRefreshSession()
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
        await ConfirmPasswordResetPasswordAsync(passwordHasher, passwordResetTokenStorage, unitOfWork, accountUid);
        var refreshToken = await CreateSessionAsync(authTokenCodec, accountStorage, unitOfWork);

        var command = new RefreshSessionCommand
        {
            RefreshToken = refreshToken
        };
        var handler = new RefreshSessionHandler(
            sessionRepository: unitOfWork.SessionRepository,
            accountStorage: accountStorage,
            unitOfWork: unitOfWork,
            refreshTokenHasher: new StubRefreshTokenHasher(),
            authTokenCodec: authTokenCodec
        );

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        var accessTokenPayload = await authTokenCodec.DecodeAccessTokenAsync(response.AccessToken);
        Assert.Equal(accountUid, accessTokenPayload.AccountUid);
        Assert.Equal("Alice", accessTokenPayload.Nickname);
        var refreshTokenPayload = await authTokenCodec.DecodeRefreshTokenAsync(response.RefreshToken);
        Assert.Equal(accountUid, refreshTokenPayload.AccountUid);
        Assert.Equal("Alice", refreshTokenPayload.Nickname);
        Assert.True(DateTime.UtcNow + new TimeSpan(0, 15, 0) >= response.AccessTokenExpiresAt);
        Assert.True(DateTime.UtcNow + new TimeSpan(30, 0, 0, 0) >= response.RefreshTokenExpiresAt);

        var events = unitOfWork.EventDispatcher.GetDispatchedEvents();
        Assert.Single(events);
        var sessionRefreshedEvent = Assert.IsType<SessionRefreshedEvent>(events[0]);
        Assert.Equal(new SessionUid(accessTokenPayload.SessionUid), sessionRefreshedEvent.SessionUid);
    }

    [Fact]
    public async Task HandleAsync_SessionDoesNotExist_ShouldThrowException()
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
        await ConfirmPasswordResetPasswordAsync(passwordHasher, passwordResetTokenStorage, unitOfWork, accountUid);
        var refreshTokenPayload = new RefreshTokenPayload
        {
            SessionUid = Guid.NewGuid(),
            AccountUid = accountUid,
            Nickname = "Alice",
            ExpiresAt = DateTime.UtcNow + new TimeSpan(30, 0, 0)
        };
        var refreshToken = await authTokenCodec.EncodeRefreshTokenAsync(refreshTokenPayload);

        var command = new RefreshSessionCommand
        {
            RefreshToken = refreshToken
        };
        var handler = new RefreshSessionHandler(
            sessionRepository: unitOfWork.SessionRepository,
            accountStorage: accountStorage,
            unitOfWork: unitOfWork,
            refreshTokenHasher: new StubRefreshTokenHasher(),
            authTokenCodec: authTokenCodec
        );

        // Act
        var exc = await Assert.ThrowsAsync<WrongAuthTokenException>(async () =>
            await handler.HandleAsync(command));

        // Assert
        Assert.Equal("The token is wrong", exc.Message);
    }

    private async Task ConfirmPasswordResetPasswordAsync(
        StubPasswordHasher passwordHasher,
        StubPasswordResetTokenStorage passwordResetTokenStorage,
        StubUnitOfWork unitOfWork,
        Guid accountUid,
        string password = "P@$$w0rd"
    )
    {
        var token = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);

        var command = new ConfirmPasswordResetCommand
        {
            Token = token,
            Password = password
        };
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
        var command = new CreateSessionCommand
        {
            Email = email,
            Password = password
        };
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

        var accountUid = (await authTokenCodec.DecodeAccessTokenAsync(response.AccessToken)).AccountUid;
        unitOfWork.EventDispatcher.ClearDispatchedEvents();

        return response.RefreshToken;
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var identityRepository = new StubIdentityRepository();
        var sessionRepository = new StubSessionRepository();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(identityRepository, sessionRepository, eventDispatcher);
    }
}
