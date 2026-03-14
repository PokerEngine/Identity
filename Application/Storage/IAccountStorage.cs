namespace Application.Storage;

public interface IAccountStorage
{
    Task<bool> ExistsAsync(Guid accountUid);
    Task<AccountView> GetViewAsync(Guid accountUid);
    Task<AccountView> GetViewByEmailAsync(string email);
    Task SaveViewAsync(AccountView accountView);
}

public record AccountView
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
    public required string Email { get; init; }
}
