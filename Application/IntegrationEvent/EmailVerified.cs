using Application.Service.MessageSender;
using Application.Storage;

namespace Application.IntegrationEvent;

public record EmailVerifiedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid AccountUid { get; init; }
}

public class EmailVerifiedHandler(
    IAccountStorage accountStorage,
    IPasswordResetTokenStorage passwordResetTokenStorage,
    IMessageSender messageSender
) : IIntegrationEventHandler<EmailVerifiedIntegrationEvent>
{
    public async Task HandleAsync(EmailVerifiedIntegrationEvent integrationEvent)
    {
        var view = await accountStorage.GetViewAsync(integrationEvent.AccountUid);
        view.IsEmailVerified = true;
        await accountStorage.SaveViewAsync(view);

        var token = await passwordResetTokenStorage.GenerateTokenAsync(integrationEvent.AccountUid);
        var message = new Message
        {
            Header = "Initialize password",
            Content = $"[Initialize password](/identity/password-reset?token={token})" // TODO: implement URL template
        };
        var recipient = new Recipient
        {
            Email = view.Email
        };
        await messageSender.SendAsync(message, recipient);
    }
}
