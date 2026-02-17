namespace Domain.ValueObject;

public readonly struct AccountUid : IEquatable<AccountUid>
{
    private readonly Guid _guid;

    public AccountUid(Guid guid)
    {
        _guid = guid;
    }

    public static implicit operator Guid(AccountUid a)
        => a._guid;

    public static implicit operator AccountUid(Guid a)
        => new(a);

    public static bool operator ==(AccountUid a, AccountUid b)
        => a._guid == b._guid;

    public static bool operator !=(AccountUid a, AccountUid b)
        => a._guid != b._guid;

    public bool Equals(AccountUid other)
        => _guid.Equals(other._guid);

    public override bool Equals(object? o)
        => o is not null && o.GetType() == GetType() && _guid.Equals(((AccountUid)o)._guid);

    public override string ToString()
        => _guid.ToString();

    public override int GetHashCode()
        => _guid.GetHashCode();
}
