using System.Globalization;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public partial class SubmodelTemplateService(
    ITemplateProvider templateProvider,
    ISubmodelTemplateMappingProvider submodelTemplateMappingProvider) : ISubmodelTemplateService
{
    private readonly ITemplateProvider _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));

    private readonly ISubmodelTemplateMappingProvider _submodelTemplateMappingProvider =
        submodelTemplateMappingProvider ?? throw new ArgumentNullException(nameof(submodelTemplateMappingProvider));

    public async Task<ISubmodel> GetSubmodelTemplateAsync(string submodelId, CancellationToken cancellationToken)
    {
        try
        {
            ValidateSubmodelId(submodelId);

            var templateId = _submodelTemplateMappingProvider.GetTemplateId(submodelId);

            return await _templateProvider.GetSubmodelTemplateAsync(templateId!, cancellationToken).ConfigureAwait(false);
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

    public async Task<ISubmodel> GetSubmodelTemplateAsync(string submodelId, string idShortPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idShortPath);
        try
        {
            ValidateSubmodelId(submodelId);

            var templateId = _submodelTemplateMappingProvider.GetTemplateId(submodelId);
            var submodel = await _templateProvider.GetSubmodelTemplateAsync(templateId!, cancellationToken).ConfigureAwait(false);

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

    private static ISubmodel BuildSubmodel(ISubmodel submodel, string idShortPath)
    {
        ArgumentNullException.ThrowIfNull(submodel);
        ArgumentException.ThrowIfNullOrWhiteSpace(idShortPath);

        var idShortPathSegments = idShortPath.Split('.');
        var currentSubmodelElements = submodel.SubmodelElements;

        foreach (var (idShort, isLast) in idShortPathSegments.Select((idShort, index) => (idShort, index == idShortPathSegments.Length - 1)))
        {
            if (TryParseIdShortWithBracketIndex(idShort, out var idShortWithoutIndex, out var index))
            {
                var listElement = GetListElementByIdShort(currentSubmodelElements!, idShortWithoutIndex);
                var selectedElement = GetElementAtIndex(listElement, index);

                listElement.Value = [selectedElement!];

                if (isLast)
                {
                    return submodel;
                }

                currentSubmodelElements = GetChildElements(selectedElement!) as List<ISubmodelElement>
                                          ?? throw new InternalDataProcessingException();
                continue;
            }

            var matchedSubmodelElement = FindMatchingElement(currentSubmodelElements!, idShort);

            matchedSubmodelElement.IdShort = idShort;
            _ = currentSubmodelElements!.RemoveAll(x => x.IdShort != idShort);

            if (isLast)
            {
                return submodel;
            }

            currentSubmodelElements = GetChildElements(matchedSubmodelElement) as List<ISubmodelElement>
                                          ?? throw new InternalDataProcessingException();
        }

        throw new NotFoundException("Template not found");
    }

    private static ISubmodelElement FindMatchingElement(IEnumerable<ISubmodelElement> submodelElements, string idShort)
    {
        var idShortWithoutIndex = SubmodelElementCollectionIndex().Replace(idShort, "");
        return submodelElements.FirstOrDefault(e => e.IdShort == idShort || e.IdShort == idShortWithoutIndex)
               ?? throw new InternalDataProcessingException();
    }

    private static bool TryParseIdShortWithBracketIndex(string idShort, out string idShortWithoutIndex, out int index)
    {
        var match = SubmodelElementListIndex().Match(idShort);
        if (!match.Success)
        {
            idShortWithoutIndex = string.Empty;
            index = -1;
            return false;
        }

        idShortWithoutIndex = match.Groups[1].Value;
        var indexGroup = match.Groups[2].Success ? match.Groups[2] : match.Groups[3];
        if (!indexGroup.Success)
        {
            idShortWithoutIndex = string.Empty;
            index = -1;
            return false;
        }

        index = int.Parse(indexGroup.Value, CultureInfo.InvariantCulture);
        return true;
    }

    private static SubmodelElementList GetListElementByIdShort(List<ISubmodelElement> elements, string idShort)
    {
        var element = elements.FirstOrDefault(e => e.IdShort == idShort);
        if (element is not SubmodelElementList list)
        {
            throw new InternalDataProcessingException();
        }

        _ = elements.RemoveAll(e => e.IdShort != idShort);
        return list;
    }

    private static ISubmodelElement? GetElementAtIndex(SubmodelElementList list, int index)
    {
        if (index < 0)
        {
            throw new InternalDataProcessingException();
        }

        if (list.TypeValueListElement is AasSubmodelElements.SubmodelElementCollection or AasSubmodelElements.SubmodelElementList && list.Value?.Count > 0)
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
            _ => null
        };
    }

    /// <summary>
    /// Matches strings like "element[3]" and captures:
    ///   Group 1 → element name (any characters, lazy match)
    ///   Group 2 → index (digits inside square brackets)
    /// e.g. "element[3]" -> matches Group1= "element", Group2 = "3"
    /// Pattern: ^(.+?)\[(\d+)\]$
    /// </summary>
    [GeneratedRegex(@"^(.+?)(?:\[(\d+)\]|%5B(\d+)%5D)$")]
    private static partial Regex SubmodelElementListIndex();

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
