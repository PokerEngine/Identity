using Domain.Event;

namespace Application.Event;

public class PasswordChangedEventHandler : IEventHandler<PasswordChangedEvent>
{
    public Task HandleAsync(PasswordChangedEvent @event, EventContext context)
    {
        // TODO: send a notification about the password change
        return Task.CompletedTask;
    }
}
