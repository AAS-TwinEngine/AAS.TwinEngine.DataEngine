using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;

public class SemanticTreeExtractor(
    ISemanticIdResolver semanticIdResolver,
    ISubmodelElementHelper elementHelper,
    IEnumerable<ISubmodelElementTypeHandler> handlers,
    ILogger<SemanticTreeExtractor> logger) : ISemanticTreeExtractor
{
    public SemanticTreeNode Extract(ISubmodel submodelTemplate)
    {
        if (submodelTemplate == null)
        {
            throw new InvalidDependencyException(nameof(submodelTemplate), logger);
        }

        var rootNode = new SemanticBranchNode(semanticIdResolver.ResolveSemanticId(submodelTemplate, submodelTemplate.IdShort!), Cardinality.Unknown);
        var childNodes = submodelTemplate.SubmodelElements!
                                         .Select(ExtractElement)
                                         .Where(childNode => childNode != null)
                                         .ToList();

        foreach (var childNode in childNodes)
        {
            rootNode.AddChild(childNode!);
        }

        return rootNode;
    }

    public ISubmodelElement Extract(ISubmodel submodelTemplate, string idShortPath)
    {
        if (submodelTemplate == null)
        {
            throw new InvalidDependencyException(nameof(submodelTemplate), logger);
        }

        if (idShortPath == null)
        {
            throw new InvalidDependencyException(nameof(idShortPath), logger);
        }

        var currentSubmodelElements = submodelTemplate.SubmodelElements;
        var idShortPathSegments = idShortPath.Split('.');
        for (var index = 0; index < idShortPathSegments.Length; index++)
        {
            var currentIdShort = idShortPathSegments[index];
            var isLastSegment = index == idShortPathSegments.Length - 1;

            var matchedElement = elementHelper.GetElementByIdShort(currentSubmodelElements, currentIdShort)
                                 ?? throw new InternalDataProcessingException();
            if (isLastSegment)
            {
                return matchedElement;
            }

            currentSubmodelElements = elementHelper.GetChildElements(matchedElement) as List<ISubmodelElement>
                                      ?? throw new InternalDataProcessingException();
        }

        throw new InternalDataProcessingException();
    }

    public SemanticTreeNode? ExtractElement(ISubmodelElement element)
    {
        if (element == null)
        {
            throw new InvalidDependencyException(nameof(element), logger);
        }

        var handler = handlers.FirstOrDefault(h => h.CanHandle(element));
        return handler != null
                   ? handler.Extract(element, ExtractElement)
                   : CreateLeafNode(element);
    }

    private SemanticLeafNode CreateLeafNode(ISubmodelElement element)
    {
        var semanticId = semanticIdResolver.ResolveElementSemanticId(element, element.IdShort!);
        var valueType = semanticIdResolver.GetValueType(element);
        var cardinality = semanticIdResolver.GetCardinality(element);
        return new SemanticLeafNode(semanticId, string.Empty, valueType, cardinality);
    }
}
