using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Bstring : IBobject, IReadOnlyList<byte>, IEquatable<Bstring>, IComparable<Bstring>
{
    private readonly ImmutableArray<byte> _bytes;

    public byte[] Value => _bytes.ToArray();

    public Bstring(byte[] bytes)
    {
        _bytes = bytes.ToImmutableArray();
    }

    public Bstring(string s, Encoding encoding)
    {
        _bytes = encoding.GetBytes(s).ToImmutableArray();
    }

    #region Interfaces implementation
    public byte this[int index] => _bytes[index];

    public int Count => _bytes.Length;

    public int CompareTo(Bstring? other)
    {
        if (other == null) return 1;
        if (ReferenceEquals(this, other)) return 0;

        var minLength = Math.Min(_bytes.Length, other._bytes.Length);
        for (int i = 0; i < minLength; i++)
        {
            var comparison = _bytes[i].CompareTo(other._bytes[i]);
            if (comparison != 0) 
                return comparison;
        }
        return _bytes.Length.CompareTo(other._bytes.Length);
    }

    public bool Equals(Bstring? other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        return _bytes.SequenceEqual(other._bytes);
    }

    public IEnumerator<byte> GetEnumerator()
    {
        return _bytes.AsEnumerable().GetEnumerator();
    }

    public byte[] ToBinaryEncoding()
    {
        var length = Encoding.ASCII.GetBytes($"{_bytes.Length}:");
        var bytes = new byte[length.Length + _bytes.Length];
        length.CopyTo(bytes, 0);
        _bytes.CopyTo(bytes, length.Length);
        return bytes;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    public override bool Equals(object? obj)
    {
        return obj is Bstring other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override string ToString()
    {
        return $"{_bytes.Length}:{Encoding.Latin1.GetString(_bytes.ToArray())}";
    }

    public string ToHexString()
    {
        return $"{_bytes.Length}:{BitConverter.ToString(_bytes.ToArray())}";
    }
}
