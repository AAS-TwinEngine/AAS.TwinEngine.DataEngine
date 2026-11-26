using System.Globalization;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public partial class SubmodelTemplateService(
    ITemplateProvider templateProvider,
    ISubmodelTemplateMappingProvider submodelTemplateMappingProvider,
    ILogger<SubmodelTemplateService> logger) : ISubmodelTemplateService
{
    private readonly ITemplateProvider _templateProvider = templateProvider ?? throw new InvalidDependencyException(nameof(templateProvider));

    private readonly ISubmodelTemplateMappingProvider _submodelTemplateMappingProvider =
        submodelTemplateMappingProvider ?? throw new InvalidDependencyException(nameof(submodelTemplateMappingProvider), logger);

    public async Task<ISubmodel> GetSubmodelTemplateAsync(string submodelId, CancellationToken cancellationToken)
    {
        try
        {
            ValidateSubmodelId(submodelId);

            var templateId = _submodelTemplateMappingProvider.GetTemplateId(submodelId);

            return await _templateProvider.GetFilteredSubmodelTemplateAsync(templateId!, null, cancellationToken).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new SubmodelNotFoundException(ex, submodelId);
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (RequestTimeoutException ex)
        {
            throw new TemplateRequestFailedException(ex);
        }
        catch (ServiceUnavailableException ex)
        {
            throw new RepositoryNotAvailableException(ex);
        }
    }

    public async Task<ISubmodel> GetSubmodelTemplateAsync(string submodelId, string idShortPath, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(idShortPath))
        {
            try
            {
                ValidateSubmodelId(submodelId);

                var templateId = _submodelTemplateMappingProvider.GetTemplateId(submodelId);
                var submodel = await _templateProvider.GetFilteredSubmodelTemplateAsync(templateId, queryOptions, cancellationToken).ConfigureAwait(false);

                return BuildSubmodel(submodel, idShortPath);
            }
            catch (ResourceNotFoundException ex)
            {
                throw new SubmodelElementNotFoundException(ex, submodelId);
            }
            catch (ResponseParsingException ex)
            {
                throw new InternalDataProcessingException(ex);
            }
            catch (RequestTimeoutException ex)
            {
                throw new TemplateRequestFailedException(ex);
            }
            catch (ServiceUnavailableException ex)
            {
                throw new RepositoryNotAvailableException(ex);
            }
        }
        throw new InvalidDependencyException(nameof(idShortPath));
    }

    public async Task<bool> ValidateSemanticIdFilter(string submodelId, string filteredTemplateId)
    {
        ValidateSubmodelId(submodelId);

        string? templateId;
        try
        {
            templateId = _submodelTemplateMappingProvider.GetTemplateId(submodelId);

            if (filteredTemplateId is not null && templateId != filteredTemplateId)
            {
                return false;
            }
            return true;
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
    }

    public async Task<ISubmodel?> GetFilteredSubmodelTemplateAsync(string submodelId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        ValidateSubmodelId(submodelId);

        string? templateId;
        try
        {
            templateId = _submodelTemplateMappingProvider.GetTemplateId(submodelId);

            return await _templateProvider.GetFilteredSubmodelTemplateAsync(templateId!, queryOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (RequestTimeoutException ex)
        {
            throw new TemplateRequestFailedException(ex);
        }
        catch (ServiceUnavailableException ex)
        {
            throw new RepositoryNotAvailableException(ex);
        }
    }

    public async Task<string?> GetFilteredSubmodelTemplateIdAsync(string semanticId, CancellationToken cancellationToken)
    {
        try
        {
            var submodelTemplate = await _templateProvider.GetFilteredSubmodelTemplateBySemanticIdAsync(semanticId, cancellationToken).ConfigureAwait(false);
            return submodelTemplate?.Id;
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (RequestTimeoutException ex)
        {
            throw new TemplateRequestFailedException(ex);
        }
        catch (ServiceUnavailableException ex)
        {
            throw new RepositoryNotAvailableException(ex);
        }
    }

    private static ISubmodel BuildSubmodel(ISubmodel submodel, string idShortPath)
    {
        if (submodel is null)
        {
            throw new InvalidDependencyException(nameof(submodel));
        }

        if (string.IsNullOrWhiteSpace(idShortPath))
        {
            throw new InvalidDependencyException(nameof(idShortPath));
        }

        var idShortPathSegments = idShortPath.Split('.');
        IEnumerable<ISubmodelElement>? currentSubmodelElements = submodel.SubmodelElements;

        foreach (var (rawSegment, isLast) in idShortPathSegments.Select((segment, index) => (segment, index == idShortPathSegments.Length - 1)))
        {
            var segment = IdShortPathSegment.Parse(rawSegment);
            ISubmodelElement selectedElement;

            if (segment.HasIndices)
            {
                selectedElement = GetListElementByIdShort(currentSubmodelElements!, segment.IdShort);
                foreach (var index in segment.Indices)
                {
                    var list = selectedElement as SubmodelElementList ?? throw new InternalDataProcessingException();
                    selectedElement = GetElementAtIndex(list, index) ?? throw new InternalDataProcessingException();
                }
            }
            else
            {
                selectedElement = FindMatchingElement(currentSubmodelElements!, rawSegment);
                selectedElement.IdShort = rawSegment;
                RetainOnly(currentSubmodelElements!, rawSegment);
            }

            if (isLast)
            {
                return submodel;
            }

            currentSubmodelElements = GetChildElements(selectedElement) ?? throw new InternalDataProcessingException();
        }

        throw new TemplateNotFoundException();
    }

    private static ISubmodelElement FindMatchingElement(IEnumerable<ISubmodelElement> submodelElements, string idShort)
    {
        var idShortWithoutIndex = SubmodelElementCollectionIndex().Replace(idShort, "");
        return submodelElements.FirstOrDefault(e => e.IdShort == idShort || e.IdShort == idShortWithoutIndex)
               ?? throw new SubmodelElementNotFoundException();
    }

    private static SubmodelElementList GetListElementByIdShort(IEnumerable<ISubmodelElement> elements, string idShort)
    {
        var element = elements.FirstOrDefault(e => e.IdShort == idShort);
        if (element is not SubmodelElementList list)
        {
            throw new InternalDataProcessingException();
        }

        RetainOnly(elements, idShort);
        return list;
    }

    // Annotations are typed List<IDataElement>, so pruning has to handle both container element types.
    private static void RetainOnly(IEnumerable<ISubmodelElement> elements, string idShort)
    {
        switch (elements)
        {
            case List<ISubmodelElement> submodelElements:
                _ = submodelElements.RemoveAll(e => e.IdShort != idShort);
                break;
            case List<IDataElement> dataElements:
                _ = dataElements.RemoveAll(e => e.IdShort != idShort);
                break;
            default:
                throw new InternalDataProcessingException();
        }
    }

    private static ISubmodelElement? GetElementAtIndex(SubmodelElementList list, int index)
    {
        if (index < 0)
        {
            throw new InternalDataProcessingException();
        }

        if (list.TypeValueListElement is AasSubmodelElements.SubmodelElementCollection or AasSubmodelElements.SubmodelElementList && list.Value?.Count == 1)
        {
            if (GetCardinality(list.Value.FirstOrDefault()!) is Cardinality.OneToMany or Cardinality.ZeroToMany)
            {
                return list.Value.FirstOrDefault()!;
            }
        }

        if (index >= list.Value?.Count)
        {
            throw new InternalDataProcessingException();
        }

        return list.Value?[index];
    }

    private static IEnumerable<ISubmodelElement>? GetChildElements(ISubmodelElement element)
    {
        return element switch
        {
            ISubmodelElementCollection collection => collection.Value,
            ISubmodelElementList list => list.Value,
            IEntity entity => entity.Statements,
            IAnnotatedRelationshipElement annotated => annotated.Annotations,
            _ => null
        };
    }

    /// <summary>
    /// Matches one or more digits at the end of a string,
    /// e.g., "sensor42" → matches "42"
    /// Pattern: \d+$
    /// </summary>
    [GeneratedRegex(@"\d+$")]
    private static partial Regex SubmodelElementCollectionIndex();

    private static void ValidateSubmodelId(string submodelId)
    {
        if (string.IsNullOrWhiteSpace(submodelId))
        {
            throw new InternalDataProcessingException();
        }
    }

    private static Cardinality GetCardinality(ISubmodelElement element)
    {
        var qualifierValue = element.Qualifiers?.FirstOrDefault()?.Value;
        if (qualifierValue is null)
        {
            return Cardinality.Unknown;
        }

        return Enum.TryParse<Cardinality>(qualifierValue, ignoreCase: true, out var result)
                   ? result
                   : Cardinality.Unknown;
    }
}
