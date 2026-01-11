using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet;

public sealed class Bencode
{
    private const byte IntegerBeginCharacter = (byte)'i';
    private const byte ListBeginCharacter = (byte)'l';
    private const byte DictionaryBeginCharacter = (byte)'d';
    private const byte TerminationCharacter = (byte)'e';
    private const byte NumberPaddingCharacter = (byte)'0';
    private const byte StringPaddingCharacter = (byte)':';

    public static IBobject Decode(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes), "Byte array is null");
        if (bytes.Length == 0) throw new ArgumentException("Byte array is empty");

        switch (bytes[0])
        {
            case IntegerBeginCharacter:
                return DecodeInteger(bytes, 1);
            case ListBeginCharacter:
                return DecodeList(bytes, 1);
            case DictionaryBeginCharacter:
                return DecodeDictionary(bytes, 1);
            default:
                if ((bytes[0] - NumberPaddingCharacter) >= 10)
                    throw new ArgumentException("Invalid character at position: 0");
                return DecodeString(bytes, 0);
        }
    }

    private static Binteger DecodeInteger(byte[] bytes, int startAt = 0)
    {
        var isNegative = false;
        var value = 0L;
        for (int i = startAt; bytes[i] != TerminationCharacter; i++)
        {
            var character = bytes[i];
            if (Convert.ToChar(character) == '-')
            {
                if (isNegative)
                    throw new ArgumentException($"Invalid character at position: {i}");

                isNegative = true;
                continue;
            }

            var digit = character - NumberPaddingCharacter;
            if (digit >= 0 && digit <= 9)
            {
                value = (value * 10) + digit;
                continue;
            }

            throw new ArgumentException($"Invalid character at position: {i}");
        }
        var result = isNegative ? -value : value;
        return new Binteger(result);
    }

    private static Bstring DecodeString(byte[] bytes, int startAt = 0)
    {
        var length = 0;
        var i = startAt;
        for (; bytes[i] != StringPaddingCharacter; i++)
        {
            var character = bytes[i];
            var digit = character - NumberPaddingCharacter;
            if (digit >= 0 && digit <= 9)
            {
                length = (length * 10) + digit;
            }
            else
                throw new ArgumentException($"Invalid character at position: {i}");
        }

        var content = new List<byte>();
        var endAt = i + length;
        for (i++; i < endAt; i++)
        {
            var b = bytes[i];
            content.Add(b);
        }
        return new Bstring(content.ToArray());
    }

    private static Blist DecodeList(byte[] bytes, int startAt = 0)
    {
        var list = new List<IBobject>();
        return new Blist(list);
    }

    private static Bdictionary DecodeDictionary(byte[] bytes, int startAt = 0)
    {
        var dictionary = new Dictionary<Bstring, IBobject>();
        return new Bdictionary(dictionary);
    }
}
