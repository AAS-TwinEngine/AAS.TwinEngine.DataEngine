using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class ReferenceElementHandler(
    ISemanticIdResolver semanticIdResolver,
    IReferenceHelper referenceHelper,
    ILogger<ReferenceElementHandler> logger) : ISubmodelElementTypeHandler
{
    public bool CanHandle(ISubmodelElement element) => element is ReferenceElement;

    public SemanticTreeNode? Extract(ISubmodelElement element, Func<ISubmodelElement, SemanticTreeNode?> extractChild)
    {
        var referenceElement = (ReferenceElement)element;

        if (referenceElement.Value == null || referenceElement.Value.Type == ReferenceTypes.ExternalReference)
        {
            return null;
        }

        return referenceHelper.ExtractReferenceKeys(
            referenceElement.Value,
            semanticIdResolver.ResolveElementSemanticId(referenceElement, referenceElement.IdShort!),
            semanticIdResolver.GetCardinality(referenceElement));
    }

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        var referenceElement = (ReferenceElement)element;

        if (referenceElement?.Value?.Type != ReferenceTypes.ModelReference)
        {
            logger.LogInformation("ReferenceElement does not contain a ModelReference for SemanticId '{SemanticId}'. Skipping population.", semanticIdResolver.ExtractSemanticId(referenceElement!));
            return;
        }

        referenceHelper.PopulateReferenceKeys(referenceElement.Value, values, semanticIdResolver.ExtractSemanticId(referenceElement));
    }
}
