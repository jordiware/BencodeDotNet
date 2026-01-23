using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BencodeNameAttribute : Attribute
{
    public Bstring Name { get; }

    public BencodeNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = new Bstring(name, Encoding.UTF8);
    }
}
