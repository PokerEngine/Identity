using Application.Exception;
using Application.Service.PasswordEncryptor;
using Domain.ValueObject;

namespace Application.Test.Service.PasswordEncryptor;

public class StubPasswordEncryptor : IPasswordEncryptor
{
    public Task<EncryptedPassword> EncryptPasswordAsync(Password password)
    {
        // For the stub implementation, we just reverse the string
        return Task.FromResult(new EncryptedPassword(Reverse(password)));
    }

    public Task ValidatePasswordAsync(Password password, EncryptedPassword encryptedPassword)
    {
        if (Reverse(password) != encryptedPassword)
        {
            throw new WrongPasswordException("The password is wrong");
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
