namespace Application.Storage;

public interface IAccountStorage
{
    Task<AccountView> GetViewAsync(Guid accountUid);
    Task<bool> AccountExistsAsync(Guid accountUid);
    Task SaveViewAsync(AccountView accountView);
}

public record AccountView
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
    public required string Email { get; init; }
}
