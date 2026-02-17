namespace Domain.ValueObject;

public readonly struct EncryptedPassword : IEquatable<EncryptedPassword>
{
    private readonly string _value;

    public EncryptedPassword(string value)
    {
        _value = value;
    }

    public static implicit operator string(EncryptedPassword a)
        => a._value;

    public static implicit operator EncryptedPassword(string a)
        => new(a);

    public bool Equals(EncryptedPassword other)
        => _value.Equals(other._value);

    public override string ToString()
        => "********";
}
