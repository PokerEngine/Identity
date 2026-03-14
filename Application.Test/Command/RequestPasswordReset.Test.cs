using Application.Command;
using Application.Storage;
using Application.Test.Service.MessageSender;
using Application.Test.Storage;

namespace Application.Test.Command;

public class RequestPasswordResetTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldSendEmail()
    {
        // Arrange
        var accountUid = Guid.NewGuid();
        var messageSender = new StubMessageSender();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var accountStorage = new StubAccountStorage();
        var accountView = new AccountView
        {
            Uid = accountUid,
            Nickname = "Test",
            Email = "test@test.com"
        };
        await accountStorage.SaveViewAsync(accountView);

        var command = new RequestPasswordResetCommand
        {
            Email = "test@test.com"
        };
        var handler = new RequestPasswordResetHandler(
            accountStorage: accountStorage,
            passwordResetTokenStorage: passwordResetTokenStorage,
            messageSender: messageSender
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Single(messageSender.GetSentMessages());
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ShouldDoNothing()
    {
        // Arrange
        var messageSender = new StubMessageSender();
        var passwordResetTokenStorage = new StubPasswordResetTokenStorage(new TimeSpan(0, 5, 0));
        var accountStorage = new StubAccountStorage();

        var command = new RequestPasswordResetCommand
        {
            Email = "test@test.com"
        };
        var handler = new RequestPasswordResetHandler(
            accountStorage: accountStorage,
            passwordResetTokenStorage: passwordResetTokenStorage,
            messageSender: messageSender
        );

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Empty(messageSender.GetSentMessages());
    }
}
