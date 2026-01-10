using System.Collections;

namespace Jordiware.BencodeDotNet;

public sealed class Blist : IBobject, IEnumerable<IBobject>
{
    private IBobject[] objects { get; set; } = [];

    #region Interfaces implementation
    public IEnumerator<IBobject> GetEnumerator()
    {
        yield return (IBobject)objects.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion
}
