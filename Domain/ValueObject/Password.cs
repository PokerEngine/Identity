using Domain.Exception;

namespace Domain.ValueObject;

public readonly struct Password
{
    private readonly string _value;

    private const int MinLength = 8;
    private const int MaxLength = 32;

    public Password(string value)
    {
        value = value.Trim();

        if (value.Length < MinLength)
        {
            throw new InvalidPasswordException($"Password must contain at least {MinLength} symbol(s)");
        }
        if (value.Length > MaxLength)
        {
            throw new InvalidPasswordException($"Password must not contain more than {MaxLength} symbol(s)");
        }

        _value = value;
    }

    public static implicit operator string(Password a)
        => a._value;

    public static implicit operator Password(string a)
        => new(a);

    public override string ToString()
        => "********";
}
