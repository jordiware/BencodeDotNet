namespace Jordiware.BencodeDotNet;

public static class Bencode
{
    public const byte IntegerBeginCharacter = (byte)'i';
    public const byte ListBeginCharacter = (byte)'l';
    public const byte DictionaryBeginCharacter = (byte)'d';
    public const byte TerminationCharacter = (byte)'e';
    public const byte MinNumberCharacter = (byte)'0';
    public const byte MaxNumberCharacter = (byte)'9';
    public const byte StringPaddingCharacter = (byte)':';
}
