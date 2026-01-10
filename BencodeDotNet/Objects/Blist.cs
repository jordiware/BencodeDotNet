using System.Collections;
using System.Collections.Immutable;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Blist : IBobject, IReadOnlyList<IBobject>
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
        return (IEnumerator<IBobject>)_objects.ToArray().GetEnumerator();
    }

    public byte[] ToBinaryEncoding()
    {
        var encoded = new List<byte>([ (byte)'l' ]);
        foreach (IBobject o in _objects)
        {
            encoded.AddRange(o.ToBinaryEncoding());
        }
        encoded.Add((byte)'e');
        return encoded.ToArray();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    public override string ToString()
    {
        return $"l{string.Join(string.Empty, _objects)}e";
    }
}
