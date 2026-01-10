using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Jordiware.BencodeDotNet;

public sealed class Bdictionary : IBobject, IReadOnlyDictionary<Bstring, IBobject>
{
    private ReadOnlyDictionary<Bstring, IBobject> keyValuePairs { get; set; } = ReadOnlyDictionary<Bstring, IBobject>.Empty;

    #region Interfaces implementation
    public IBobject this[Bstring key] => keyValuePairs[key];

    public IEnumerable<Bstring> Keys => keyValuePairs.Keys;

    public IEnumerable<IBobject> Values => keyValuePairs.Values;

    public int Count => keyValuePairs.Count;

    public bool ContainsKey(Bstring key)
    {
        return keyValuePairs.ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<Bstring, IBobject>> GetEnumerator()
    {
        return keyValuePairs.GetEnumerator();
    }

    public bool TryGetValue(Bstring key, [MaybeNullWhen(false)] out IBobject value)
    {
        return keyValuePairs.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion
}
