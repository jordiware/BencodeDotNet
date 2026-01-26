namespace Jordiware.BencodeDotNet.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BencodeIgnoreAttribute : Attribute
{
}
