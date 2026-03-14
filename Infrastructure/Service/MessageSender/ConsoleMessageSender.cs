using Application.Service.MessageSender;

namespace Infrastructure.Service.MessageSender;

public class ConsoleMessageSender(ILogger<ConsoleMessageSender> logger) : IMessageSender
{
    public Task SendAsync(Message message, Recipient recipient)
    {
        logger.LogInformation(@$"========
Sending message to {recipient}:
**{message.Header}**
{message.Content}
========");
        return Task.CompletedTask;
    }
}
