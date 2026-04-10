using Application.Exception;
using Application.Service.RefreshTokenHasher;
using Domain.ValueObject;

namespace Application.Test.Service.RefreshTokenHasher;

public class StubRefreshTokenHasher : IRefreshTokenHasher
{
    public Task<RefreshTokenHash> HashAsync(string refreshToken)
    {
        // For the stub implementation, we just reverse the string
        return Task.FromResult(new RefreshTokenHash(Reverse(refreshToken)));
    }

    public Task VerifyAsync(string refreshToken, RefreshTokenHash refreshTokenHash)
    {
        if (Reverse(refreshToken) != refreshTokenHash)
        {
            throw new WrongAuthTokenException("The token is wrong");
        }

        return Task.CompletedTask;
    }

    private string Reverse(string str)
    {
        var chars = str.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}
