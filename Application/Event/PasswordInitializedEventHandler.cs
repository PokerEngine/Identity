using Domain.Event;

namespace Application.Event;

public class PasswordInitializedEventHandler : IEventHandler<PasswordInitializedEvent>
{
    public Task HandleAsync(PasswordInitializedEvent @event, EventContext context)
    {
        // TODO: send a notification about the password initialization
        return Task.CompletedTask;
    }
}
