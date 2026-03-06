using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;

public interface ISubmodelFiller
{
    ISubmodel FillOutTemplate(ISubmodel submodelTemplate, SemanticTreeNode values);
}
