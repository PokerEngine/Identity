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
using Domain.Exception;
using Domain.ValueObject;

namespace Application.Test.Command;

public class ChangePasswordTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldChangePassword()
    {
        // Arrange
        var passwordEncryptor = new StubPasswordEncryptor();
        var unitOfWork = CreateUnitOfWork();
        var accountUid = Guid.NewGuid();
        await InitializePasswordAsync(passwordEncryptor, unitOfWork, accountUid, "P@$$w0rd");

        var command = new ChangePasswordCommand
        {
            AccountUid = accountUid,
            OldPassword = "P@$$w0rd",
            NewPassword = "P@$$w0rd-new"
        };
        var handler = new ChangePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: unitOfWork.AccountStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        var identity = Identity.FromEvents(accountUid, await unitOfWork.Repository.GetEventsAsync(accountUid));
        Assert.Equal(new EncryptedPassword("wen-dr0w$$@P"), identity.EncryptedPassword);

        var events = await unitOfWork.EventDispatcher.GetDispatchedEvents(accountUid);
        Assert.Single(events);
        Assert.IsType<PasswordChangedEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_NotInitialized_ShouldThrowException()
    {
        // Arrange
        var passwordEncryptor = new StubPasswordEncryptor();
        var unitOfWork = CreateUnitOfWork();
        var accountUid = Guid.NewGuid();
        var accountView = new AccountView
        {
            Uid = accountUid,
            Nickname = "Test",
            Email = "test@test.com"
        };
        await unitOfWork.AccountStorage.SaveViewAsync(accountView);

        var command = new ChangePasswordCommand
        {
            AccountUid = accountUid,
            OldPassword = "P@$$w0rd",
            NewPassword = "P@$$w0rd-new"
        };
        var handler = new ChangePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: unitOfWork.AccountStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );

        // Act
        var exc = await Assert.ThrowsAsync<PasswordNotInitializedException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The account password is not initialized", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ShouldThrowException()
    {
        // Arrange
        var passwordEncryptor = new StubPasswordEncryptor();
        var unitOfWork = CreateUnitOfWork();
        var accountUid = Guid.NewGuid();
        await InitializePasswordAsync(passwordEncryptor, unitOfWork, accountUid, "P@$$w0rd");

        var command = new ChangePasswordCommand
        {
            AccountUid = accountUid,
            OldPassword = "P@$$w0rd-wrong",
            NewPassword = "P@$$w0rd-new"
        };
        var handler = new ChangePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: unitOfWork.AccountStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );

        // Act
        var exc = await Assert.ThrowsAsync<WrongPasswordException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The password is wrong", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ShouldThrowException()
    {
        // Arrange
        var passwordEncryptor = new StubPasswordEncryptor();
        var unitOfWork = CreateUnitOfWork();

        var command = new ChangePasswordCommand
        {
            AccountUid = Guid.NewGuid(),
            OldPassword = "P@$$w0rd",
            NewPassword = "P@$$w0rd-new"
        };
        var handler = new ChangePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: unitOfWork.AccountStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );

        // Act
        var exc = await Assert.ThrowsAsync<AccountNotFoundException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The account is not found", exc.Message);
    }

    private async Task InitializePasswordAsync(
        StubPasswordEncryptor passwordEncryptor,
        StubUnitOfWork unitOfWork,
        Guid accountUid,
        string password
    )
    {
        var accountView = new AccountView
        {
            Uid = accountUid,
            Nickname = "Test",
            Email = "test@test.com"
        };
        await unitOfWork.AccountStorage.SaveViewAsync(accountView);

        var handler = new InitializePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: unitOfWork.AccountStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );
        var command = new InitializePasswordCommand
        {
            AccountUid = accountUid,
            Password = password
        };
        await handler.HandleAsync(command);
        await unitOfWork.EventDispatcher.ClearDispatchedEvents(accountUid);
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var repository = new StubRepository();
        var accountStorage = new StubAccountStorage();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(repository, accountStorage, eventDispatcher);
    }
}
