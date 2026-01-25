using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet.Objects;

/// <summary>
/// Represents a Bencode dictionary.
/// </summary>
/// <remarks>
/// A Bencode dictionary is a collection of key-value pairs where
/// keys are Bencode byte strings and values are arbitrary Bencode objects.
/// The dictionary is encoded as the ASCII character <c>'d'</c>,
/// followed by each key-value pair encoded in lexicographical key order,
/// and terminated by the character <c>'e'</c>.
/// <para>
/// Dictionary keys are ordered using lexicographical byte comparison
/// of their underlying byte sequences, as defined by the Bencode
/// specification.
/// </para>
/// <para>
/// Example:
/// <c>d3:cow3:moo4:spam4:eggse</c>
/// </para>
/// </remarks>
public sealed class Bdictionary : IBobject, IReadOnlyDictionary<Bstring, IBobject>, IEquatable<Bdictionary>
{
    private readonly ImmutableSortedDictionary<Bstring, IBobject> _keyValuePairs = ImmutableSortedDictionary<Bstring, IBobject>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bdictionary"/> class
    /// from the specified key-value pairs.
    /// </summary>
    /// <param name="keyValuePairs">
    /// The dictionary entries to include. Keys must be unique.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="keyValuePairs"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Entries are stored in canonical lexicographical order
    /// of their keys.
    /// </remarks>
    public Bdictionary(IDictionary<Bstring, IBobject> keyValuePairs)
    {
        _keyValuePairs = keyValuePairs.ToImmutableSortedDictionary();
    }

    #region Interfaces implementation
    /// <summary>
    /// Gets the Bencode object associated with the specified key.
    /// </summary>
    /// <param name="key">
    /// The Bencode string key.
    /// </param>
    public IBobject this[Bstring key] => _keyValuePairs[key];

    /// <summary>
    /// Gets a collection containing the keys of the dictionary.
    /// </summary>
    public IEnumerable<Bstring> Keys => _keyValuePairs.Keys;

    /// <summary>
    /// Gets a collection containing the values of the dictionary.
    /// </summary>
    public IEnumerable<IBobject> Values => _keyValuePairs.Values;

    /// <summary>
    /// Gets the number of key-value pairs contained in the dictionary.
    /// </summary>
    public int Count => _keyValuePairs.Count;

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">
    /// The key to locate in the dictionary.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the dictionary contains the specified key;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsKey(Bstring key)
    {
        return _keyValuePairs.ContainsKey(key);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the key-value pairs
    /// of the dictionary in canonical key order.
    /// </summary>
    public IEnumerator<KeyValuePair<Bstring, IBobject>> GetEnumerator()
    {
        return _keyValuePairs.GetEnumerator();
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">
    /// The key whose value to retrieve.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the value associated with
    /// the specified key, if the key is found; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the dictionary contains the specified key;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetValue(Bstring key, [MaybeNullWhen(false)] out IBobject value)
    {
        return _keyValuePairs.TryGetValue(key, out value);
    }

    /// <summary>
    /// Determines whether the current <see cref="Bdictionary"/> is equal to
    /// another <see cref="Bdictionary"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="Bdictionary"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if both dictionaries contain the same keys
    /// and all corresponding values are equal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Equality is independent of insertion order and is evaluated
    /// based on canonical key ordering.
    /// </remarks>
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

    /// <summary>
    /// Serializes the current <see cref="Bdictionary"/> into its binary
    /// Bencode representation.
    /// </summary>
    /// <returns>
    /// A byte array containing the canonical Bencode encoding
    /// of this dictionary.
    /// </returns>
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

    /// <summary>
    /// Computes the encoded length of this dictionary in Bencode format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dictionaries are encoded as <c>d&lt;key&gt;&lt;value&gt;...e</c>, where keys
    /// are Bencode strings and entries are encoded sequentially in lexicographical
    /// key order.
    /// </para>
    /// <para>
    /// The encoded length is therefore equal to:
    /// </para>
    /// <list type="bullet">
    ///   <item>1 byte for the leading <c>'d'</c></item>
    ///   <item>The sum of the encoded lengths of all keys and values</item>
    ///   <item>1 byte for the trailing <c>'e'</c></item>
    /// </list>
    /// </remarks>
    /// <returns>
    /// The number of bytes required to encode this dictionary.
    /// </returns>
    public int GetEncodedLength()
    {
        var length = 2;

        foreach ((var key, var value) in _keyValuePairs)
        {
            length += key.GetEncodedLength();
            length += value.GetEncodedLength();
        }

        return length;
    }

    /// <summary>
    /// Writes the Bencode dictionary representation of this value to the specified <see cref="PipeWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the Bencode data will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while writing asynchronously.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The dictionary is encoded using the standard Bencode format:
    /// <c>d&lt;key&gt;&lt;value&gt;...e</c>.
    /// </para>
    /// <para>
    /// Both keys and values are written using their respective
    /// <see cref="IBobject.WriteToPipeAsync"/> implementations.
    /// </para>
    /// </remarks>
    public async Task WriteToPipeAsync(PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteAsync(new[] { Bencode.DictionaryBeginCharacter }, cancellationToken);

        foreach (var (key, value) in _keyValuePairs)
        {
            await key.WriteToPipeAsync(writer, cancellationToken);
            await value.WriteToPipeAsync(writer, cancellationToken);
        }

        await writer.WriteAsync(new[] { Bencode.TerminationCharacter }, cancellationToken);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Bdictionary other && Equals(other);
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Returns the canonical Bencode string representation of the dictionary.
    /// </summary>
    /// <remarks>
    /// The output reflects canonical key ordering and is intended
    /// primarily for diagnostics and debugging.
    /// </remarks>
    /// <returns>
    /// A string in the form <c>d&lt;key&gt;&lt;value&gt;...e</c>.
    /// </returns>
    public override string ToString()
    {
        var kvps = _keyValuePairs.Select(kvp => $"{kvp.Key}{kvp.Value}").ToArray();
        return $"d{string.Join(string.Empty, kvps)}e";
    }
}
