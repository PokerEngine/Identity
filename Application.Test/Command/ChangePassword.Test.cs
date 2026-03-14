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
        var accountUid = Guid.NewGuid();
        var passwordEncryptor = new StubPasswordEncryptor();
        var accountStorage = new StubAccountStorage();
        var unitOfWork = CreateUnitOfWork();
        await InitializePasswordAsync(passwordEncryptor, accountStorage, unitOfWork, accountUid, "P@$$w0rd");

        var command = new ChangePasswordCommand
        {
            AccountUid = accountUid,
            OldPassword = "P@$$w0rd",
            NewPassword = "P@$$w0rd-new"
        };
        var handler = new ChangePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: accountStorage,
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
        var accountUid = Guid.NewGuid();
        var passwordEncryptor = new StubPasswordEncryptor();
        var accountStorage = new StubAccountStorage();
        var unitOfWork = CreateUnitOfWork();
        var accountView = new AccountView
        {
            Uid = accountUid,
            Nickname = "Test",
            Email = "test@test.com"
        };
        await accountStorage.SaveViewAsync(accountView);

        var command = new ChangePasswordCommand
        {
            AccountUid = accountUid,
            OldPassword = "P@$$w0rd",
            NewPassword = "P@$$w0rd-new"
        };
        var handler = new ChangePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: accountStorage,
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
        var accountUid = Guid.NewGuid();
        var passwordEncryptor = new StubPasswordEncryptor();
        var accountStorage = new StubAccountStorage();
        var unitOfWork = CreateUnitOfWork();
        await InitializePasswordAsync(passwordEncryptor, accountStorage, unitOfWork, accountUid, "P@$$w0rd");

        var command = new ChangePasswordCommand
        {
            AccountUid = accountUid,
            OldPassword = "P@$$w0rd-wrong",
            NewPassword = "P@$$w0rd-new"
        };
        var handler = new ChangePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: accountStorage,
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
        var accountStorage = new StubAccountStorage();
        var unitOfWork = CreateUnitOfWork();

        var command = new ChangePasswordCommand
        {
            AccountUid = Guid.NewGuid(),
            OldPassword = "P@$$w0rd",
            NewPassword = "P@$$w0rd-new"
        };
        var handler = new ChangePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: accountStorage,
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
        StubAccountStorage accountStorage,
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
        await accountStorage.SaveViewAsync(accountView);

        var handler = new InitializePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: accountStorage,
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
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(repository, eventDispatcher);
    }
}
