using System.Collections;
using System.Collections.Immutable;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Blist : IBobject, IReadOnlyList<IBobject>
{
    private readonly ImmutableArray<IBobject> objects;

    #region Interfaces implementation
    public IBobject this[int index] => objects[index];

    public int Count => objects.Length;

    public IEnumerator<IBobject> GetEnumerator()
    {
        return (IEnumerator<IBobject>)objects.ToArray().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    public override string ToString()
    {
        return $"l{string.Join(string.Empty, objects)}e";
    }
}
