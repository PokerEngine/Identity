using Application.Exception;
using Application.Service.MessageSender;
using Application.Storage;

namespace Application.Command;

public record RequestPasswordResetCommand : ICommand
{
    public required string Email { get; init; }
}

public record RequestPasswordResetResponse : ICommandResponse;

public class RequestPasswordResetHandler(
    IAccountStorage accountStorage,
    IPasswordResetTokenStorage passwordResetTokenStorage,
    IMessageSender messageSender
) : ICommandHandler<RequestPasswordResetCommand, RequestPasswordResetResponse>
{
    public async Task<RequestPasswordResetResponse> HandleAsync(RequestPasswordResetCommand command)
    {
        AccountView view;

        try
        {
            view = await accountStorage.GetViewByEmailAsync(command.Email);

            if (!view.IsEmailVerified)
            {
                throw new AccountNotFoundException("Email not verified");
            }
        }
        catch (AccountNotFoundException)
        {
            // Due to security reasons, we don't let the user know whether such account exists
            return new RequestPasswordResetResponse();
        }

        await passwordResetTokenStorage.DeleteTokensAsync(view.AccountUid); // Delete all previous requests

        var token = await passwordResetTokenStorage.GenerateTokenAsync(view.AccountUid);
        var message = new Message
        {
            Header = "Reset password",
            Content = $"[Reset password](/identity/password-reset?token={token})" // TODO: implement URL template
        };
        var recipient = new Recipient
        {
            Email = view.Email
        };
        await messageSender.SendAsync(message, recipient);

        return new RequestPasswordResetResponse();
    }
}
