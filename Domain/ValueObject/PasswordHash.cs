namespace Domain.ValueObject;

public readonly struct PasswordHash
{
    private readonly string _value;

    public PasswordHash(string value)
    {
        _value = value;
    }

    public static implicit operator string(PasswordHash a)
        => a._value;

    public static implicit operator PasswordHash(string a)
        => new(a);

    public override string ToString()
        => "********";
}
