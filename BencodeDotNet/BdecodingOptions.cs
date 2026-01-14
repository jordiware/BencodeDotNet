namespace Jordiware.BencodeDotNet;

public record BdecodingOptions(
    int MaxDepth = 1024,
    int MaxStringLength = 64 * 1024 * 1024, // 64 MB
    int MaxContainerItems = 1024);
