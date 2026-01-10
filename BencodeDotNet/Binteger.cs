namespace Jordiware.BencodeDotNet;

public sealed class Binteger : IBobject, IEquatable<Binteger>, IComparable<Binteger>
{
    public long Value { get; private set; } = 0;

    public int CompareTo(Binteger? other)
    {
        if (other == null) return 1;
        
        return Value.CompareTo(other.Value);
    }

    public bool Equals(Binteger? other)
    {
        if (other == null) return false;

        return Value.Equals(other.Value);
    }
}
