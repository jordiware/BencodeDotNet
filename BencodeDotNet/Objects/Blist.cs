using System.Collections;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Blist : IBobject, IReadOnlyList<IBobject>
{
    private IBobject[] objects { get; set; } = [];

    #region Interfaces implementation
    public IBobject this[int index] => objects[index];

    public int Count => objects.Length;

    public IEnumerator<IBobject> GetEnumerator()
    {
        yield return (IBobject)objects.GetEnumerator();
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
