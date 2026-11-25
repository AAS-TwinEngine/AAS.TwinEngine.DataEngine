using System.Globalization;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

/// <summary>
/// Builds the ModelReference of a submodel element by walking its idShortPath through the submodel,
/// adding one key per element on the way (IDTA-01002 Content=Reference).
/// </summary>
public static partial class SubmodelElementReferenceBuilder
{
    public static IReference Build(ISubmodel submodel, string submodelId, string idShortPath)
    {
        ArgumentNullException.ThrowIfNull(submodel);
        ArgumentException.ThrowIfNullOrWhiteSpace(idShortPath);

        var keys = new List<IKey> { new Key(KeyTypes.Submodel, submodelId) };
        IEnumerable<ISubmodelElement>? currentElements = submodel.SubmodelElements;

        foreach (var segment in idShortPath.Split('.'))
        {
            var match = ListIndexSegment().Match(segment);
            var idShort = match.Success ? match.Groups[1].Value : segment;

            var element = currentElements?.FirstOrDefault(e => string.Equals(e.IdShort, idShort, StringComparison.Ordinal))
                          ?? throw new SubmodelElementNotFoundException(idShortPath);
            keys.Add(new Key(element.ToKeyType(), idShort));

            if (match.Success)
            {
                var index = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                if (element is not ISubmodelElementList list || list.Value is null || index >= list.Value.Count)
                {
                    throw new SubmodelElementNotFoundException(idShortPath);
                }

                element = list.Value[index];
                keys.Add(new Key(element.ToKeyType(), index.ToString(CultureInfo.InvariantCulture)));
            }

            currentElements = GetChildElements(element);
        }

        return keys.ToModelReference();
    }

    private static IEnumerable<ISubmodelElement>? GetChildElements(ISubmodelElement element) => element switch
    {
        ISubmodelElementCollection collection => collection.Value,
        ISubmodelElementList list => list.Value,
        IEntity entity => entity.Statements,
        IAnnotatedRelationshipElement annotated => annotated.Annotations,
        _ => null
    };

    [GeneratedRegex(@"^(.+?)\[(\d+)\]$")]
    private static partial Regex ListIndexSegment();
}
