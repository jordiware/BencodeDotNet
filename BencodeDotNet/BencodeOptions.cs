namespace Jordiware.BencodeDotNet;

public struct BencodeOptions()
{
    public readonly int MaxDepth = 1024;
    public readonly int MaxStringLength = 64 * 1024 * 1024;
    public readonly int MaxContainerItems = 1024;

    public BencodeOptions(int maxDepth, int maxStringLength, int maxContainerItems) : this()
    {
        MaxDepth = maxDepth;
        MaxStringLength = maxStringLength;
        MaxContainerItems = maxContainerItems;
    }
}
