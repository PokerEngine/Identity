using Application.Exception;
using Application.Service.PasswordHasher;
using Domain.ValueObject;
using System.Security.Cryptography;

namespace Infrastructure.Service.PasswordHasher;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128-bit
    private const int HashSize = 32; // 256-bit
    private const int Iterations = 100_000;

    public Task<PasswordHash> HashAsync(Password password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256
        );

        var hash = pbkdf2.GetBytes(HashSize);

        var combined = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);

        var encoded = Convert.ToBase64String(combined);

        return Task.FromResult(new PasswordHash(encoded));
    }

    public Task VerifyAsync(
        Password password,
        PasswordHash passwordHash)
    {
        var combined = Convert.FromBase64String(passwordHash);

        var salt = new byte[SaltSize];
        var storedHash = new byte[HashSize];

        Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(combined, SaltSize, storedHash, 0, HashSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        var computedHash = pbkdf2.GetBytes(HashSize);

        if (!CryptographicOperations.FixedTimeEquals(storedHash, computedHash))
        {
            throw new WrongPasswordException("The password is wrong");
        }

        return Task.CompletedTask;
    }
}
