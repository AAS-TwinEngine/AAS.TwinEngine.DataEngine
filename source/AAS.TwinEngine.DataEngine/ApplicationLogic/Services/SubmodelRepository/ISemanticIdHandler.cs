using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface ISemanticIdHandler
{
    SemanticTreeNode Extract(ISubmodel submodelTemplate);

    ISubmodelElement Extract(ISubmodel submodelTemplate, string idShortPath);

    ISubmodel FillOutTemplate(ISubmodel submodelTemplate, SemanticTreeNode values);
}
