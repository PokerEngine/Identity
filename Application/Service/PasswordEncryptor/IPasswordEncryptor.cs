using Domain.ValueObject;

namespace Application.Service.PasswordEncryptor;

public interface IPasswordEncryptor
{
    Task<EncryptedPassword> EncryptPasswordAsync(Password password);
    Task ValidatePasswordAsync(Password password, EncryptedPassword encryptedPassword);
}
