using Application.Command;
using Application.Exception;
using Application.Storage;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.AuthTokenCodec;
using Application.Test.Service.PasswordHasher;
using Application.Test.Service.RefreshTokenHasher;
using Application.Test.Storage;
using Application.Test.UnitOfWork;
using Domain.Entity;
using Domain.Event;

namespace Application.Test.Command;

public class CreateSessionTest
{
    [Fact]
    public async Task HandleAsync_CredentialsMatch_ShouldCreateSession()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var accountStorage = new StubAccountStorage();
        var passwordHasher = new StubPasswordHasher();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var authTokenCodec = new StubAuthTokenCodec();
        var refreshTokenHasher = new StubRefreshTokenHasher();
        var unitOfWork = CreateUnitOfWork();
        var account = new AccountView
        {
            AccountUid = accountUid,
            Nickname = "Alice",
            Email = "alice.alright@test.com",
            IsEmailVerified = true
        };
        await accountStorage.SaveViewAsync(account);
        await ConfirmPasswordResetPasswordAsync(passwordHasher, passwordResetTokenStorage, unitOfWork, accountUid, "P@$$w0rd");

        var command = new CreateSessionCommand
        {
            Email = "alice.alright@test.com",
            Password = "P@$$w0rd"
        };
        var handler = new CreateSessionHandler(
            identityRepository: unitOfWork.IdentityRepository,
            sessionRepository: unitOfWork.SessionRepository,
            accountStorage: accountStorage,
            unitOfWork: unitOfWork,
            passwordHasher: new StubPasswordHasher(),
            refreshTokenHasher: refreshTokenHasher,
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

        var session = Session.FromEvents(accessTokenPayload.SessionUid, await unitOfWork.SessionRepository.GetEventsAsync(accessTokenPayload.SessionUid));
        var refreshTokenHash = await refreshTokenHasher.HashAsync(response.RefreshToken);
        Assert.Equal(refreshTokenHash, session.RefreshTokenHash);

        var events = unitOfWork.EventDispatcher.GetDispatchedEvents();
        Assert.Single(events);
        var sessionCreatedEvent = Assert.IsType<SessionCreatedEvent>(events[0]);
        Assert.Equal(session.Uid, sessionCreatedEvent.SessionUid);
    }

    [Fact]
    public async Task HandleAsync_EmailDoesNotMatch_ShouldThrowException()
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

        var command = new CreateSessionCommand
        {
            Email = "alice.alright-wrong@test.com",
            Password = "P@$$w0rd"
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

        // Act
        var exc = await Assert.ThrowsAsync<WrongCredentialsException>(async () =>
            await handler.HandleAsync(command));

        // Assert
        Assert.Equal("The credentials are wrong", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_PasswordDoesNotMatch_ShouldThrowException()
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

        var command = new CreateSessionCommand
        {
            Email = "alice.alright@test.com",
            Password = "P@$$w0rd-wrong"
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

        // Act
        var exc = await Assert.ThrowsAsync<WrongCredentialsException>(async () =>
            await handler.HandleAsync(command));

        // Assert
        Assert.Equal("The credentials are wrong", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_AccountDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var accountStorage = new StubAccountStorage();
        var passwordHasher = new StubPasswordHasher();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var authTokenCodec = new StubAuthTokenCodec();
        var unitOfWork = CreateUnitOfWork();
        await ConfirmPasswordResetPasswordAsync(passwordHasher, passwordResetTokenStorage, unitOfWork, accountUid);

        var command = new CreateSessionCommand
        {
            Email = "alice.alright@test.com",
            Password = "P@$$w0rd"
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

        // Act
        var exc = await Assert.ThrowsAsync<WrongCredentialsException>(async () =>
            await handler.HandleAsync(command));

        // Assert
        Assert.Equal("The credentials are wrong", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_PasswordIsNotSet_ShouldThrowException()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var accountStorage = new StubAccountStorage();
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

        var command = new CreateSessionCommand
        {
            Email = "alice.alright@test.com",
            Password = "P@$$w0rd"
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

        // Act
        var exc = await Assert.ThrowsAsync<WrongCredentialsException>(async () =>
            await handler.HandleAsync(command));

        // Assert
        Assert.Equal("The credentials are wrong", exc.Message);
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

    private StubUnitOfWork CreateUnitOfWork()
    {
        var identityRepository = new StubIdentityRepository();
        var sessionRepository = new StubSessionRepository();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(identityRepository, sessionRepository, eventDispatcher);
    }
}
