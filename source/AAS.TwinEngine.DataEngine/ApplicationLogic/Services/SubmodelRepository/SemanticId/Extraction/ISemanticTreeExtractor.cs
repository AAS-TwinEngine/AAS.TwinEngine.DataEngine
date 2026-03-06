using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;

public interface ISemanticTreeExtractor
{
    SemanticTreeNode Extract(ISubmodel submodelTemplate);

    ISubmodelElement Extract(ISubmodel submodelTemplate, string idShortPath);
}
