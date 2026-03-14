namespace Application.Service.MessageSender;

public interface IMessageSender
{
    Task SendAsync(Message message, Recipient recipient);
}

public record Message
{
    public required string Header { init; get; }
    public required string Content { init; get; }
}

public record Recipient
{
    public required string Email { init; get; }
}
