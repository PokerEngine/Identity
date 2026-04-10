using Domain.ValueObject;

namespace Application.Service.RefreshTokenHasher;

public interface IRefreshTokenHasher
{
    Task<RefreshTokenHash> HashAsync(string refreshToken);
    Task VerifyAsync(string refreshToken, RefreshTokenHash refreshTokenHash);
}
