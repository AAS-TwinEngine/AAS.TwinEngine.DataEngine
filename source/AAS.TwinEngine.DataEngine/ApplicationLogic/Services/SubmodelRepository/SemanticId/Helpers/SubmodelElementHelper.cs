using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public class SubmodelElementHelper(ILogger<SubmodelElementHelper> logger, IOptions<PluginsConfig> pluginsConfig) : ISubmodelElementHelper
{
    private readonly HashSet<string>? _defaultLanguagesSet = pluginsConfig.Value.MultiLanguageProperty.DefaultLanguages is { Count: > 0 }
                                                                ? new HashSet<string>(pluginsConfig.Value.MultiLanguageProperty.DefaultLanguages, StringComparer.OrdinalIgnoreCase)
                                                                : null;

    public ISubmodelElement CloneElement(ISubmodelElement element) => CloneSubmodelElement(element);

    public IReadOnlyList<ISubmodelElement> CloneElements(ISubmodelElement element, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        var clones = new ISubmodelElement[count];
        for (var i = 0; i < count; i++)
        {
            clones[i] = CloneSubmodelElement(element);
        }

        return clones;
    }

    public ISubmodelElement? GetElementByIdShort(IEnumerable<ISubmodelElement>? submodelElements, string idShort)
    {
        var segment = IdShortPathSegment.Parse(idShort);
        if (!segment.HasIndices)
        {
            return submodelElements?.FirstOrDefault(e => e.IdShort == idShort);
        }

        var element = GetElementFromListByIndex(submodelElements, segment.IdShort, segment.Indices[0]);
        foreach (var index in segment.Indices.Skip(1))
        {
            element = GetListItem(element, segment.IdShort, index);
        }

        return element;
    }

    public ISubmodelElement GetElementFromListByIndex(IEnumerable<ISubmodelElement>? elements, string idShortWithoutIndex, int index)
        => GetListItem(elements?.FirstOrDefault(e => e.IdShort == idShortWithoutIndex), idShortWithoutIndex, index);

    private ISubmodelElement GetListItem(ISubmodelElement? baseElement, string idShortWithoutIndex, int index)
    {
        if (baseElement is not ISubmodelElementList list)
        {
            logger.LogError("Expected list element with IdShort '{IdShortWithoutIndex}' not found or is not a list.", idShortWithoutIndex);
            throw new InternalDataProcessingException();
        }

        if (index >= 0 && index < list.Value!.Count)
        {
            return list.Value[index];
        }

        logger.LogError("Index {Index} is out of bounds for list '{IdShortWithoutIndex}' with count {Count}.", index, idShortWithoutIndex, list.Value!.Count);
        throw new InternalDataProcessingException();
    }

    public IList<ISubmodelElement>? GetChildElements(ISubmodelElement submodelElement)
    {
        return submodelElement switch
        {
            ISubmodelElementCollection c => c.Value,
            ISubmodelElementList l => l.Value,
            IEntity entity => entity.Statements,
            // Annotations are List<IDataElement>; a navigation-only copy is returned because IList<T> is invariant.
            IAnnotatedRelationshipElement annotated => annotated.Annotations?.Cast<ISubmodelElement>().ToList(),
            _ => null
        };
    }

    public HashSet<string> ResolveLanguages(MultiLanguageProperty mlp)
    {
        var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (mlp.Value is { Count: > 0 })
        {
            foreach (var langValue in mlp.Value)
            {
                _ = languages.Add(langValue.Language);
            }
        }

        if (_defaultLanguagesSet != null)
        {
            languages.UnionWith(_defaultLanguagesSet);
        }

        return languages;
    }

    private static ISubmodelElement CloneSubmodelElement(ISubmodelElement element)
      => Jsonization.Deserialize.ISubmodelElementFrom(Jsonization.Serialize.ToJsonObject(element));
}
