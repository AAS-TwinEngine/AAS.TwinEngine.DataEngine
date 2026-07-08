using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

public sealed class SubmodelQueryOptions(string? level, string? extent)
{
    public string? Level { get; } = level;
    public string? Extent { get; } = extent;
}
