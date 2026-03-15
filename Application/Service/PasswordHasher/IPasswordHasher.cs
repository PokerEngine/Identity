using Domain.ValueObject;

namespace Application.Service.PasswordHasher;

public interface IPasswordHasher
{
    Task<PasswordHash> HashAsync(Password password);
    Task VerifyAsync(Password password, PasswordHash passwordHash);
}
