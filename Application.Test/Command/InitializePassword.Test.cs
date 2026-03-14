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

public class InitializePasswordTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldInitializePassword()
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

        var command = new InitializePasswordCommand
        {
            AccountUid = accountUid,
            Password = "P@$$w0rd"
        };
        var handler = new InitializePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: accountStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        var identity = Identity.FromEvents(accountUid, await unitOfWork.Repository.GetEventsAsync(accountUid));
        Assert.Equal(new AccountUid(accountUid), identity.AccountUid);
        Assert.Equal(new EncryptedPassword("dr0w$$@P"), identity.EncryptedPassword);

        var events = await unitOfWork.EventDispatcher.GetDispatchedEvents(accountUid);
        Assert.Single(events);
        Assert.IsType<PasswordInitializedEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_AlreadyInitialized_ShouldThrowException()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var passwordEncryptor = new StubPasswordEncryptor();
        var accountStorage = new StubAccountStorage();
        var unitOfWork = CreateUnitOfWork();
        await InitializePasswordAsync(passwordEncryptor, accountStorage, unitOfWork, accountUid, "P@$$w0rd");

        var command = new InitializePasswordCommand
        {
            AccountUid = accountUid,
            Password = "P@$$w0rd"
        };
        var handler = new InitializePasswordHandler(
            repository: unitOfWork.Repository,
            accountStorage: accountStorage,
            passwordEncryptor: passwordEncryptor,
            unitOfWork: unitOfWork
        );

        // Act
        var exc = await Assert.ThrowsAsync<PasswordInitializedException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The account password is already initialized", exc.Message);
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ShouldThrowException()
    {
        // Arrange
        var passwordEncryptor = new StubPasswordEncryptor();
        var accountStorage = new StubAccountStorage();
        var unitOfWork = CreateUnitOfWork();

        var command = new InitializePasswordCommand
        {
            AccountUid = Guid.NewGuid(),
            Password = "P@$$w0rd"
        };
        var handler = new InitializePasswordHandler(
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
