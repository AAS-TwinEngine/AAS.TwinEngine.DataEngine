using System.Collections.Concurrent;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public class SemanticIdHandler(ISemanticTreeExtractor extractor, ISubmodelFiller filler) : ISemanticIdHandler
{
    private readonly ConcurrentDictionary<string, SemanticTreeNode> _extractedTemplates = new(StringComparer.Ordinal);

    public SemanticTreeNode Extract(ISubmodel submodelTemplate)
    {
        ArgumentNullException.ThrowIfNull(submodelTemplate);

        var templateId = submodelTemplate.Administration?.TemplateId;
        if (string.IsNullOrWhiteSpace(templateId))
        {
            templateId = submodelTemplate.Id;
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            return extractor.Extract(submodelTemplate);
        }

        return _extractedTemplates.GetOrAdd(templateId, _ => extractor.Extract(submodelTemplate));
    }

    public ISubmodelElement Extract(ISubmodel submodelTemplate, string idShortPath) => extractor.Extract(submodelTemplate, idShortPath);

    public ISubmodel FillOutTemplate(ISubmodel submodelTemplate, SemanticTreeNode values) => filler.FillOutTemplate(submodelTemplate, values);
}
