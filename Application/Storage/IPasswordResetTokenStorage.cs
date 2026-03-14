namespace Application.Storage;

public interface IPasswordResetTokenStorage
{
    Task<string> GenerateTokenAsync(Guid accountUid);
    Task<Guid> VerifyTokenAsync(string token);
    Task DeleteTokensAsync(Guid accountUid);
}
