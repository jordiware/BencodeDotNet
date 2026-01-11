using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Bdictionary : IBobject, IReadOnlyDictionary<Bstring, IBobject>
{
    private readonly ImmutableSortedDictionary<Bstring, IBobject> _keyValuePairs = ImmutableSortedDictionary<Bstring, IBobject>.Empty;

    public Bdictionary(IDictionary<Bstring, IBobject> keyValuePairs)
    {
        _keyValuePairs = keyValuePairs.ToImmutableSortedDictionary();
    }

    #region Interfaces implementation
    public IBobject this[Bstring key] => _keyValuePairs[key];

    public IEnumerable<Bstring> Keys => _keyValuePairs.Keys;

    public IEnumerable<IBobject> Values => _keyValuePairs.Values;

    public int Count => _keyValuePairs.Count;

    public bool ContainsKey(Bstring key)
    {
        return _keyValuePairs.ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<Bstring, IBobject>> GetEnumerator()
    {
        return _keyValuePairs.GetEnumerator();
    }

    public byte[] ToBinaryEncoding()
    {
        var encoded = new List<byte>([ Bencode.DictionaryBeginCharacter ]);
        foreach (var kvp in _keyValuePairs)
        {
            encoded.AddRange(kvp.Key.ToBinaryEncoding());
            encoded.AddRange(kvp.Value.ToBinaryEncoding());
        }
        encoded.Add(Bencode.TerminationCharacter);
        return encoded.ToArray();
    }

    public bool TryGetValue(Bstring key, [MaybeNullWhen(false)] out IBobject value)
    {
        return _keyValuePairs.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    public override string ToString()
    {
        var kvps = _keyValuePairs.Select(kvp => $"{kvp.Key}{kvp.Value}").ToArray();
        return $"d{string.Join(string.Empty, kvps)}e";
    }
}
