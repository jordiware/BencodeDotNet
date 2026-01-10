using System.Text;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Binteger : IBobject, IEquatable<Binteger>, IComparable<Binteger>
{
    private readonly long value;

    public long Value => value;

    #region Interfaces implementation
    public int CompareTo(Binteger? other)
    {
        if (other == null) return 1;
        
        return value.CompareTo(other.value);
    }

    public bool Equals(Binteger? other)
    {
        if (other == null) return false;

        return value.Equals(other.value);
    }

    public byte[] ToBinaryEncoding()
    {
        return Encoding.ASCII.GetBytes(ToString());
    }
    #endregion

    public override string ToString()
    {
        return $"i{value}e";
    }
}
