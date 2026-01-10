using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Bstring : IBobject, IReadOnlyList<byte>, IEquatable<Bstring>, IComparable<Bstring>
{
    private readonly ImmutableArray<byte> bytes;

    public byte[] Value => bytes.ToArray();

    #region Interfaces implementation
    public int Count => bytes.Length;

    public byte this[int index] => bytes[index];

    public int CompareTo(Bstring? other)
    {
        if (other == null) return 1;

        var minLength = Math.Min(bytes.Length, other.bytes.Length);
        for (int i = 0; i < minLength; i++)
        {
            var comparison = bytes[i].CompareTo(other.bytes[i]);
            if (comparison != 0) 
                return comparison;
        }
        return bytes.Length.CompareTo(other.bytes.Length);
    }

    public bool Equals(Bstring? other)
    {
        if (other == null) return false;
        
        return bytes.SequenceEqual(other.bytes);
    }

    public IEnumerator<byte> GetEnumerator()
    {
        return (IEnumerator<byte>)bytes.ToArray().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    public override string ToString()
    {
        return $"{bytes.Length}:{Encoding.Latin1.GetString(bytes.ToArray())}";
    }

    public string ToHexString()
    {
        return $"{bytes.Length}:{BitConverter.ToString(bytes.ToArray())}";
    }
}
