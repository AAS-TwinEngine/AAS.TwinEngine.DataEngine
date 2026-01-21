namespace AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

public class ShellDescriptorData
{
    public string GlobalAssetId { get; set; } = null!;
    public string IdShort { get; set; } = null!;
    public string Id { get; set; } = null!;
    public List<SpecificAssetIdsData>? SpecificAssetIds { get; set; } = [];
}

public class SpecificAssetIdsData
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}
