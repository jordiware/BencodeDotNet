using System.Text;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Binteger : IBobject, IEquatable<Binteger>, IComparable<Binteger>
{
    private readonly long _value;

    public long Value => _value;

    public Binteger(long value)
    {
        _value = value;
    }

    #region Interfaces implementation
    public int CompareTo(Binteger? other)
    {
        if (other == null) return 1;
        
        return _value.CompareTo(other._value);
    }

    public bool Equals(Binteger? other)
    {
        if (other == null) return false;

        return _value.Equals(other._value);
    }

    public byte[] ToBinaryEncoding()
    {
        return Encoding.ASCII.GetBytes(ToString());
    }
    #endregion

    public override bool Equals(object? obj)
    {
        return obj is Binteger other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return $"i{_value}e";
    }
}
