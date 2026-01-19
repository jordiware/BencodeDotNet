using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides Bencode serialization and deserialization support for
/// <see cref="bool"/> values using the canonical integer representation.
/// </summary>
/// <remarks>
/// <para>
/// In Bencode, boolean values are not a native primitive. This serializer
/// represents <see cref="bool"/> values as <see cref="Binteger"/> instances,
/// following the conventional mapping:
/// </para>
/// <list type="bullet">
///   <item><description><c>false</c> → integer value <c>0</c></description></item>
///   <item><description><c>true</c> → integer value <c>1</c></description></item>
/// </list>
/// <para>
/// During deserialization, only integer values <c>0</c> and <c>1</c> are
/// considered valid boolean encodings. Any other integer value is rejected
/// and causes the operation to fail.
/// </para>
/// </remarks>
public class BoolSerializer : UnmanagedTypeBencodeSerializer<bool, Binteger>
{
    /// <summary>
    /// Attempts to serialize a <see cref="bool"/> value into its
    /// corresponding <see cref="Binteger"/> representation.
    /// </summary>
    /// <param name="input">
    /// The boolean value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains a
    /// <see cref="Binteger"/> with value <c>0</c> or <c>1</c>, depending on
    /// <paramref name="input"/>.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="bool"/> values
    /// can be represented using the integer mapping.
    /// </returns>
    public override bool TrySerialize(bool input, out Binteger output)
    {
        var value = input ? 1 : 0;
        output = new Binteger(value);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="Binteger"/> into a
    /// <see cref="bool"/> value.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the
    /// deserialized boolean value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="input"/> has a value of
    /// <c>0</c> or <c>1</c>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Integer values other than <c>0</c> and <c>1</c> are considered invalid
    /// boolean encodings and are explicitly rejected to ensure strict and
    /// predictable deserialization semantics.
    /// </remarks>
    public override bool TryDeserialize(Binteger input, out bool output)
    {
        output = default;

        var value = input.Value;
        if (value == 0)
        {
            output = false;
            return true;
        }
        if (value == 1)
        {
            output = true;
            return true;
        }

        return false;
    }
}
