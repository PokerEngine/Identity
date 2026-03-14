using Application.Service.MessageSender;

namespace Application.Test.Service.MessageSender;

public class StubMessageSender : IMessageSender
{
    private readonly List<(Message, Recipient)> _messages = new();

    public Task SendAsync(Message message, Recipient recipient)
    {
        _messages.Add((message, recipient));
        return Task.CompletedTask;
    }

    public List<(Message, Recipient)> GetSentMessages()
    {
        return _messages;
    }
}
