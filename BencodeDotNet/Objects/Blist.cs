using System.Collections;
using System.Collections.Immutable;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Blist : IBobject, IReadOnlyList<IBobject>, IEquatable<Blist>
{
    private readonly ImmutableArray<IBobject> _objects;

    public Blist(IEnumerable<IBobject> objects)
    {
        _objects = objects.ToImmutableArray();
    }

    #region Interfaces implementation
    public IBobject this[int index] => _objects[index];

    public int Count => _objects.Length;

    public IEnumerator<IBobject> GetEnumerator()
    {
        return _objects.AsEnumerable().GetEnumerator();
    }

    public bool Equals(Blist? other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Enumerable.SequenceEqual(this, other);
    }

    public byte[] ToBinaryEncoding()
    {
        var encoded = new List<byte>([ Bencode.ListBeginCharacter ]);
        foreach (IBobject o in _objects)
        {
            encoded.AddRange(o.ToBinaryEncoding());
        }
        encoded.Add(Bencode.TerminationCharacter);
        return encoded.ToArray();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    public override bool Equals(object? obj)
    {
        return obj is Blist other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var o in _objects)
        {
            hash.Add(o);
        }
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return $"l{string.Join(string.Empty, _objects)}e";
    }
}
