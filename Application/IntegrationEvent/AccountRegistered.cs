using Application.Storage;

namespace Application.IntegrationEvent;

public record AccountRegisteredIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid AccountUid { get; init; }

    public required string Nickname { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string BirthDate { get; init; }
}

public class AccountRegisteredHandler(
    IAccountStorage accountStorage
) : IIntegrationEventHandler<AccountRegisteredIntegrationEvent>
{
    public async Task HandleAsync(AccountRegisteredIntegrationEvent integrationEvent)
    {
        var accountView = new AccountView
        {
            Uid = integrationEvent.AccountUid,
            Nickname = integrationEvent.Nickname,
            Email = integrationEvent.Email
        };
        await accountStorage.SaveViewAsync(accountView);
    }
}
