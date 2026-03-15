namespace Domain.ValueObject;

public readonly struct SessionUid : IEquatable<SessionUid>
{
    private readonly Guid _guid;

    public SessionUid(Guid guid)
    {
        _guid = guid;
    }

    public static implicit operator Guid(SessionUid a)
        => a._guid;

    public static implicit operator SessionUid(Guid a)
        => new(a);

    public static bool operator ==(SessionUid a, SessionUid b)
        => a._guid == b._guid;

    public static bool operator !=(SessionUid a, SessionUid b)
        => a._guid != b._guid;

    public bool Equals(SessionUid other)
        => _guid.Equals(other._guid);

    public override bool Equals(object? o)
        => o is not null && o.GetType() == GetType() && _guid.Equals(((SessionUid)o)._guid);

    public override string ToString()
        => _guid.ToString();

    public override int GetHashCode()
        => _guid.GetHashCode();
}
