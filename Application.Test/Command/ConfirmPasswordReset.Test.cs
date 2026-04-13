using Application.Command;
using Application.Exception;
using Application.Storage;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.PasswordHasher;
using Application.Test.Storage;
using Application.Test.UnitOfWork;
using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Test.Command;

public class ConfirmPasswordResetTest
{
    [Fact]
    public async Task HandleAsync_WhenIdentityNotExists_ShouldInitializePassword()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var passwordHasher = new StubPasswordHasher();
        var accountStorage = new StubAccountStorage();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var unitOfWork = CreateUnitOfWork();
        var account = new AccountView
        {
            AccountUid = accountUid,
            Nickname = "Alice",
            Email = "alice.alright@test.com"
        };
        await accountStorage.SaveViewAsync(account);
        var token = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);

        var command = new ConfirmPasswordResetCommand
        {
            Token = token,
            Password = "P@$$w0rd"
        };
        var handler = new ConfirmPasswordResetHandler(
            identityRepository: unitOfWork.IdentityRepository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordHasher: passwordHasher,
            unitOfWork: unitOfWork
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        var identity = Identity.FromEvents(accountUid, await unitOfWork.IdentityRepository.GetEventsAsync(accountUid));
        Assert.Equal(new PasswordHash("dr0w$$@P"), identity.PasswordHash);

        var events = unitOfWork.EventDispatcher.GetDispatchedEvents();
        Assert.Single(events);
        Assert.IsType<PasswordInitializedEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityExists_ShouldChangePassword()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var passwordHasher = new StubPasswordHasher();
        var accountStorage = new StubAccountStorage();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var unitOfWork = CreateUnitOfWork();
        var account = new AccountView
        {
            AccountUid = accountUid,
            Nickname = "Alice",
            Email = "alice.alright@test.com"
        };
        await accountStorage.SaveViewAsync(account);
        await ConfirmPasswordResetPasswordAsync(passwordHasher, passwordResetTokenStorage, unitOfWork, accountUid, "P@$$w0rd-old");
        var token = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);

        var command = new ConfirmPasswordResetCommand
        {
            Token = token,
            Password = "P@$$w0rd"
        };
        var handler = new ConfirmPasswordResetHandler(
            identityRepository: unitOfWork.IdentityRepository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordHasher: passwordHasher,
            unitOfWork: unitOfWork
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        var identity = Identity.FromEvents(accountUid, await unitOfWork.IdentityRepository.GetEventsAsync(accountUid));
        Assert.Equal(new PasswordHash("dr0w$$@P"), identity.PasswordHash);

        var events = unitOfWork.EventDispatcher.GetDispatchedEvents();
        Assert.Single(events);
        Assert.IsType<PasswordChangedEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_TokenNotFound_ShouldThrowException()
    {
        // Arrange
        var passwordHasher = new StubPasswordHasher();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var unitOfWork = CreateUnitOfWork();

        var command = new ConfirmPasswordResetCommand
        {
            Token = "unknown-token",
            Password = "P@$$w0rd"
        };
        var handler = new ConfirmPasswordResetHandler(
            identityRepository: unitOfWork.IdentityRepository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordHasher: passwordHasher,
            unitOfWork: unitOfWork
        );

        // Act
        var exc = await Assert.ThrowsAsync<WrongPasswordResetTokenException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The token is not found", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_TokenExpired_ShouldThrowException()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var passwordHasher = new StubPasswordHasher();
        var accountStorage = new StubAccountStorage();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 0, 0));
        var unitOfWork = CreateUnitOfWork();
        var account = new AccountView
        {
            AccountUid = accountUid,
            Nickname = "Alice",
            Email = "alice.alright@test.com"
        };
        await accountStorage.SaveViewAsync(account);
        var token = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);

        var command = new ConfirmPasswordResetCommand
        {
            Token = token,
            Password = "P@$$w0rd"
        };
        var handler = new ConfirmPasswordResetHandler(
            identityRepository: unitOfWork.IdentityRepository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordHasher: passwordHasher,
            unitOfWork: unitOfWork
        );

        // Act
        var exc = await Assert.ThrowsAsync<WrongPasswordResetTokenException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The token is expired", exc.Message);
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
