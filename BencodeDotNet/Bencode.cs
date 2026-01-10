using Jordiware.BencodeDotNet.Objects;
using System.Text;

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
        if (bytes == null) throw new ArgumentNullException();
        if (bytes.Length == 0) throw new ArgumentException("Byte array is empty.");

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

    private static Binteger DecodeInteger(byte[] bytes, long startAt = 0)
    {
        var builder = new StringBuilder();
        for (long i = startAt; bytes[i] != TerminationCharacter; i++)
        {
            var character = bytes[i];
            if (character == '-')
            {
                builder.Append('-');
                continue;
            }

            var digit = character - NumberPaddingCharacter;
            if (digit >= 0 && digit <= 9)
            {
                builder.Append(Convert.ToChar(character));
                continue;
            }

            throw new ArgumentException($"Invalid character at position: {i}");
        }
        var result = long.Parse(builder.ToString());
        return new Binteger(result);
    }

    private static Bstring DecodeString(byte[] bytes, long startAt = 0)
    {
        throw new NotImplementedException();
    }

    private static Blist DecodeList(byte[] bytes, long startAt = 0)
    {
        var list = new List<IBobject>();
        return new Blist(list);
    }

    private static Bdictionary DecodeDictionary(byte[] bytes, long startAt = 0)
    {
        var dictionary = new Dictionary<Bstring, IBobject>();
        return new Bdictionary(dictionary);
    }
}
