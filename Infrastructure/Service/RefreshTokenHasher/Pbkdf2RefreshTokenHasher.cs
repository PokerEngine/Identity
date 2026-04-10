using Application.Exception;
using Application.Service.RefreshTokenHasher;
using Domain.ValueObject;
using System.Security.Cryptography;

namespace Infrastructure.Service.RefreshTokenHasher;

public class Pbkdf2RefreshTokenHasher : IRefreshTokenHasher
{
    private const int SaltSize = 16; // 128-bit
    private const int HashSize = 32; // 256-bit
    private const int Iterations = 100_000;

    public Task<RefreshTokenHash> HashAsync(string refreshToken)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            refreshToken,
            salt,
            Iterations,
            HashAlgorithmName.SHA256
        );

        var hash = pbkdf2.GetBytes(HashSize);

        var combined = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);

        var encoded = Convert.ToBase64String(combined);

        return Task.FromResult(new RefreshTokenHash(encoded));
    }

    public Task VerifyAsync(
        string refreshToken,
        RefreshTokenHash refreshTokenHash)
    {
        var combined = Convert.FromBase64String(refreshTokenHash);

        var salt = new byte[SaltSize];
        var storedHash = new byte[HashSize];

        Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(combined, SaltSize, storedHash, 0, HashSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            refreshToken,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        var computedHash = pbkdf2.GetBytes(HashSize);

        if (!CryptographicOperations.FixedTimeEquals(storedHash, computedHash))
        {
            throw new WrongAuthTokenException("The token is wrong");
        }

        return Task.CompletedTask;
    }
}

