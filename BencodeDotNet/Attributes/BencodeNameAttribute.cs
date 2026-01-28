using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BencodeNameAttribute : Attribute
{
    public BString Name { get; }

    public BencodeNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = new BString(name, Encoding.UTF8);
    }
}
