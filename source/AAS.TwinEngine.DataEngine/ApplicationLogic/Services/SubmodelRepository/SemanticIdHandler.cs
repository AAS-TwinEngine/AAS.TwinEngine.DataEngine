using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public class SemanticIdHandler(
    ISemanticTreeExtractor extractor,
    ISubmodelFiller filler) : ISemanticIdHandler
{
    public SemanticTreeNode Extract(ISubmodel submodelTemplate) => extractor.Extract(submodelTemplate);

    public ISubmodelElement Extract(ISubmodel submodelTemplate, string idShortPath) => extractor.Extract(submodelTemplate, idShortPath);

    public ISubmodel FillOutTemplate(ISubmodel submodelTemplate, SemanticTreeNode values) => filler.FillOutTemplate(submodelTemplate, values);
}