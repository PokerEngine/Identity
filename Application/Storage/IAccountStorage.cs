namespace Application.Storage;

public interface IAccountStorage
{
    Task<bool> ExistsAsync(Guid accountUid);
    Task<AccountView> GetViewAsync(Guid accountUid);
    Task<AccountView> GetViewByEmailAsync(string email);
    Task SaveViewAsync(AccountView view);
}

public record AccountView
{
    public required Guid AccountUid { get; init; }
    public required string Nickname { get; init; }
    public required string Email { get; init; }
    public bool IsEmailVerified { get; set; } = false;
}
