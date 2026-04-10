namespace Domain.ValueObject;

public readonly struct RefreshTokenHash
{
    private readonly string _value;

    public RefreshTokenHash(string value)
    {
        _value = value;
    }

    public static implicit operator string(RefreshTokenHash a)
        => a._value;

    public static implicit operator RefreshTokenHash(string a)
        => new(a);

    public override string ToString()
        => _value;
}
