using Application.Service.MessageSender;
using Application.Storage;
using Domain.Event;

namespace Application.Event;

public class PasswordInitializedEventHandler(
    IAccountStorage accountStorage,
    IMessageSender messageSender
) : IEventHandler<PasswordInitializedEvent>
{
    public async Task HandleAsync(PasswordInitializedEvent @event)
    {
        var view = await accountStorage.GetViewAsync(@event.AccountUid);

        var message = new Message
        {
            Header = "Password initialized",
            Content = "Your password has been initialized"
        };
        var recipient = new Recipient
        {
            Email = view.Email
        };

        await messageSender.SendAsync(message, recipient);
    }
}
