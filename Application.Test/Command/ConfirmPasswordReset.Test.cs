using Application.Command;
using Application.Exception;
using Application.Storage;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.PasswordEncryptor;
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
        var passwordEncryptor = new StubPasswordEncryptor();
        var accountStorage = new StubAccountStorage();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var unitOfWork = CreateUnitOfWork();
        var accountView = new AccountView
        {
            Uid = accountUid,
            Nickname = "Test",
            Email = "test@test.com"
        };
        await accountStorage.SaveViewAsync(accountView);
        var token = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);

        var command = new ConfirmPasswordResetCommand
        {
            Token = token,
            Password = "P@$$w0rd"
        };
        var handler = new ConfirmPasswordResetHandler(
            repository: unitOfWork.Repository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        var identity = Identity.FromEvents(accountUid, await unitOfWork.Repository.GetEventsAsync(accountUid));
        Assert.Equal(new EncryptedPassword("dr0w$$@P"), identity.EncryptedPassword);

        var events = await unitOfWork.EventDispatcher.GetDispatchedEvents(accountUid);
        Assert.Single(events);
        Assert.IsType<PasswordInitializedEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityExists_ShouldChangePassword()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var passwordEncryptor = new StubPasswordEncryptor();
        var accountStorage = new StubAccountStorage();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var unitOfWork = CreateUnitOfWork();
        var accountView = new AccountView
        {
            Uid = accountUid,
            Nickname = "Test",
            Email = "test@test.com"
        };
        await accountStorage.SaveViewAsync(accountView);
        await ConfirmPasswordResetPasswordAsync(passwordEncryptor, passwordResetTokenStorage, unitOfWork, accountUid);
        var token = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);

        var command = new ConfirmPasswordResetCommand
        {
            Token = token,
            Password = "P@$$w0rd"
        };
        var handler = new ConfirmPasswordResetHandler(
            repository: unitOfWork.Repository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        var identity = Identity.FromEvents(accountUid, await unitOfWork.Repository.GetEventsAsync(accountUid));
        Assert.Equal(new EncryptedPassword("dr0w$$@P"), identity.EncryptedPassword);

        var events = await unitOfWork.EventDispatcher.GetDispatchedEvents(accountUid);
        Assert.Single(events);
        Assert.IsType<PasswordChangedEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_TokenNotFound_ShouldThrowException()
    {
        // Arrange
        var passwordEncryptor = new StubPasswordEncryptor();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var unitOfWork = CreateUnitOfWork();

        var command = new ConfirmPasswordResetCommand
        {
            Token = "unknown-token",
            Password = "P@$$w0rd"
        };
        var handler = new ConfirmPasswordResetHandler(
            repository: unitOfWork.Repository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordEncryptor: passwordEncryptor,
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
        var passwordEncryptor = new StubPasswordEncryptor();
        var accountStorage = new StubAccountStorage();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 0, 0));
        var unitOfWork = CreateUnitOfWork();
        var accountView = new AccountView
        {
            Uid = accountUid,
            Nickname = "Test",
            Email = "test@test.com"
        };
        await accountStorage.SaveViewAsync(accountView);
        var token = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);

        var command = new ConfirmPasswordResetCommand
        {
            Token = token,
            Password = "P@$$w0rd"
        };
        var handler = new ConfirmPasswordResetHandler(
            repository: unitOfWork.Repository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordEncryptor: passwordEncryptor,
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
        StubPasswordEncryptor passwordEncryptor,
        StubPasswordResetTokenStorage passwordResetTokenStorage,
        StubUnitOfWork unitOfWork,
        Guid accountUid
    )
    {
        var token = await passwordResetTokenStorage.GenerateTokenAsync(accountUid);

        var command = new ConfirmPasswordResetCommand
        {
            Token = token,
            Password = "P@$$w0rd-old"
        };
        var handler = new ConfirmPasswordResetHandler(
            repository: unitOfWork.Repository,
            passwordResetTokenStorage: passwordResetTokenStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );
        await handler.HandleAsync(command);
        await unitOfWork.EventDispatcher.ClearDispatchedEvents(accountUid);
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(repository, eventDispatcher);
    }
}
