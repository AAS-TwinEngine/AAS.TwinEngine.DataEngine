using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

public class SubmodelElementsPage
{
    public PagingMetaData? PagingMetaData { get; set; }

    public IList<ISubmodelElement> Result { get; init; } = [];
}
