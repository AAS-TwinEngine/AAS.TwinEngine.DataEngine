using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

public class SubmodelList
{
    public PagingMetaData? PagingMetaData { get; set; }

    public IList<ISubmodel> Result { get; init; } = [];
}
