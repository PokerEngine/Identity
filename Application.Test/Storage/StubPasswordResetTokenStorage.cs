using Application.Exception;
using Application.Storage;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Application.Test.Storage;

public class StubPasswordResetTokenStorage(TimeSpan ttl) : IPasswordResetTokenStorage
{
    private const int Length = 32;
    private readonly ConcurrentDictionary<string, PasswordResetTokenEntry> _mapping = new();

    public Task<string> GenerateTokenAsync(Guid accountUid)
    {
        var token = GenerateRandomString();

        var entry = new PasswordResetTokenEntry
        {
            AccountUid = accountUid,
            Token = token,
            GeneratedAt = DateTime.UtcNow
        };

        _mapping[token] = entry;

        return Task.FromResult(token);
    }

    public Task<Guid> VerifyTokenAsync(string token)
    {
        if (_mapping.TryGetValue(token, out var entry))
        {
            if (entry.GeneratedAt + ttl < DateTime.UtcNow)
            {
                throw new WrongPasswordResetTokenException("The token is expired");
            }

            return Task.FromResult(entry.AccountUid);
        }

        throw new WrongPasswordResetTokenException("The token is not found");
    }

    public Task DeleteTokensAsync(Guid accountUid)
    {
        var tokensToDelete = _mapping
            .Where(kvp => kvp.Value.AccountUid == accountUid)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in tokensToDelete)
        {
            _mapping.TryRemove(token, out _);
        }

        return Task.CompletedTask;
    }

    private string GenerateRandomString()
    {
        var bytes = RandomNumberGenerator.GetBytes(Length / 2);
        return Convert.ToHexStringLower(bytes);
    }
}

internal class PasswordResetTokenEntry
{
    public required Guid AccountUid { get; init; }
    public required string Token { get; init; }
    public required DateTime GeneratedAt { get; init; }
}
