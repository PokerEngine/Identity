namespace Domain.ValueObject;

public readonly struct PasswordHash : IEquatable<PasswordHash>
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

    public bool Equals(PasswordHash other)
        => _value.Equals(other._value);

    public override string ToString()
        => "********";
}
