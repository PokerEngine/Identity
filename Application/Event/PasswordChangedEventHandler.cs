using Application.Service.MessageSender;
using Application.Storage;
using Domain.Event;

namespace Application.Event;

public class PasswordChangedEventHandler(
    IAccountStorage accountStorage,
    IMessageSender messageSender
) : IEventHandler<PasswordChangedEvent>
{
    public async Task HandleAsync(PasswordChangedEvent @event)
    {
        var view = await accountStorage.GetViewAsync(@event.AccountUid);

        var message = new Message
        {
            Header = "Password changed",
            Content = "Your password has been changed"
        };
        var recipient = new Recipient
        {
            Email = view.Email
        };

        await messageSender.SendAsync(message, recipient);
    }
}
