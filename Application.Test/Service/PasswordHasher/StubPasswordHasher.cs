using Application.Exception;
using Application.Service.PasswordHasher;
using Domain.ValueObject;

namespace Application.Test.Service.PasswordHasher;

public class StubPasswordHasher : IPasswordHasher
{
    public Task<PasswordHash> HashAsync(Password password)
    {
        // For the stub implementation, we just reverse the string
        return Task.FromResult(new PasswordHash(Reverse(password)));
    }

    public Task VerifyAsync(Password password, PasswordHash passwordHash)
    {
        if (Reverse(password) != passwordHash)
        {
            throw new WrongCredentialsException("The credentials are wrong");
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
