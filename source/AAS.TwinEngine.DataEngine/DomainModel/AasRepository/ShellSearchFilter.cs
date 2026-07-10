using System.Collections.Generic;
using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRepository;

public class ShellSearchFilter
{
    public IList<SpecificAssetId>? SpecificAssetIds { get; set; }

    public string? IdShort { get; set; }
}
