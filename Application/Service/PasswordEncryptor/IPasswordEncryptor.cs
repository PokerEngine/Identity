using Domain.ValueObject;

namespace Application.Service.PasswordEncryptor;

public interface IPasswordEncryptor
{
    Task<EncryptedPassword> EncryptPassword(Password password);
    Task ValidatePassword(Password password, EncryptedPassword encryptedPassword);
}
