using System.Globalization;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_0;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public partial class SubmodelElementHelper(ILogger<SubmodelElementHelper> logger, IOptions<PluginsConfig> pluginsConfig) : ISubmodelElementHelper
{
    private readonly HashSet<string>? _defaultLanguagesSet = pluginsConfig.Value.MultiLanguageProperty.DefaultLanguages is { Count: > 0 }
                                                                ? new HashSet<string>(pluginsConfig.Value.MultiLanguageProperty.DefaultLanguages, StringComparer.OrdinalIgnoreCase)
                                                                : null;

    public ISubmodelElement CloneElement(ISubmodelElement element)
    {
        var jsonElement = Jsonization.Serialize.ToJsonObject(element);

        return Jsonization.Deserialize.ISubmodelElementFrom(jsonElement);
    }

    public ISubmodelElement? GetElementByIdShort(IEnumerable<ISubmodelElement>? submodelElements, string idShort)
    {
        if (TryParseIdShortWithBracketIndex(idShort, out var idShortWithoutIndex, out var index))
        {
            return GetElementFromListByIndex(submodelElements, idShortWithoutIndex, index);
        }

        return submodelElements?.FirstOrDefault(e => e.IdShort == idShort);
    }

    public ISubmodelElement GetElementFromListByIndex(IEnumerable<ISubmodelElement>? elements, string idShortWithoutIndex, int index)
    {
        var baseElement = elements?.FirstOrDefault(e => e.IdShort == idShortWithoutIndex);

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

    /// <summary>
    /// Matches strings like "element[3]" and captures:
    ///   Group 1 → element name (any characters, lazy match)
    ///   Group 2 → index (digits inside square brackets)
    /// e.g. "element[3]" -> matches Group1= "element", Group2 = "3"
    /// Pattern: ^(.+?)\[(\d+)\]$
    /// </summary>
    [GeneratedRegex(@"^(.+?)(?:\[(\d+)\]|%5B(\d+)%5D)$")]
    private static partial Regex SubmodelElementListIndex();
}
