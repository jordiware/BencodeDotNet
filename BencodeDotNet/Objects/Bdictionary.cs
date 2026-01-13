using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Jordiware.BencodeDotNet.Objects;

public sealed class Bdictionary : IBobject, IReadOnlyDictionary<Bstring, IBobject>, IEquatable<Bdictionary>
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

    public bool TryGetValue(Bstring key, [MaybeNullWhen(false)] out IBobject value)
    {
        return _keyValuePairs.TryGetValue(key, out value);
    }

    public bool Equals(Bdictionary? other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (!Enumerable.SequenceEqual(_keyValuePairs.Keys, other._keyValuePairs.Keys))
            return false;

        foreach (var key in _keyValuePairs.Keys)
        {
            var hasValue = other.TryGetValue(key, out var value);
            if (!hasValue) return false;
            if (!_keyValuePairs[key].Equals(value)) return false;
        }
        return true;
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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    public override bool Equals(object? obj)
    {
        return obj is Bdictionary other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kvp in _keyValuePairs)
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        var kvps = _keyValuePairs.Select(kvp => $"{kvp.Key}{kvp.Value}").ToArray();
        return $"d{string.Join(string.Empty, kvps)}e";
    }
}
