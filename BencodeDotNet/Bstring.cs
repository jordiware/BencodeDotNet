namespace Jordiware.BencodeDotNet;

public sealed class Bstring : IBobject, IEquatable<Bstring>, IComparable<Bstring>
{
    public byte[] Value { get; private set; } = [];

    public int CompareTo(Bstring? other)
    {
        if (other == null) return 1;

        var minLength = Math.Min(Value.Length, other.Value.Length);
        for (int i = 0; i < minLength; i++)
        {
            var comparison = Value[i].CompareTo(other.Value[i]);
            if (comparison != 0) 
                return comparison;
        }
        return Value.Length.CompareTo(other.Value.Length);
    }

    public bool Equals(Bstring? other)
    {
        if (other == null) return false;
        
        return Value.SequenceEqual(other.Value);
    }
}
