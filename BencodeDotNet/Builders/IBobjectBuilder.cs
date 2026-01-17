using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal interface IBobjectBuilder : IDisposable
{
    IBobject ToBobject();
}
