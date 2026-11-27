using System.Globalization;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;

/// <summary>
/// Format "Path" (idShortPath serialization, IDTA-01001 Mappings): a flat, pre-order list of idShortPaths.
/// Siblings are ordered by idShort (ordinal); list items keep their index order.
/// </summary>
internal static class IdShortPathSerializer
{
    public static bool IsSupported(ISubmodelElement element)
        => element is ISubmodelElementCollection or ISubmodelElementList or IEntity or IAnnotatedRelationshipElement;

    /// <summary>IdShortPaths always start at the first submodel element, so the submodel's own idShort is not included.</summary>
    public static List<string> SerializeElements(IEnumerable<ISubmodelElement>? elements)
    {
        var paths = new List<string>();
        foreach (var element in OrderByIdShort(elements))
        {
            AppendWithDescendants(element, element.IdShort!, paths);
        }

        return paths;
    }

    public static List<string> SerializeElement(ISubmodelElement element, string idShortPath)
    {
        var paths = new List<string>();
        AppendWithDescendants(element, idShortPath, paths);
        return paths;
    }

    private static void AppendWithDescendants(ISubmodelElement element, string path, List<string> paths)
    {
        paths.Add(path);

        if (element is ISubmodelElementList list)
        {
            for (var index = 0; index < (list.Value?.Count ?? 0); index++)
            {
                AppendWithDescendants(list.Value![index], $"{path}[{index.ToString(CultureInfo.InvariantCulture)}]", paths);
            }

            return;
        }

        IEnumerable<ISubmodelElement>? children = element switch
        {
            ISubmodelElementCollection collection => collection.Value,
            IEntity entity => entity.Statements,
            IAnnotatedRelationshipElement annotated => annotated.Annotations,
            _ => null
        };

        foreach (var child in OrderByIdShort(children))
        {
            AppendWithDescendants(child, $"{path}.{child.IdShort}", paths);
        }
    }

    private static IEnumerable<ISubmodelElement> OrderByIdShort(IEnumerable<ISubmodelElement>? elements)
        => (elements ?? []).Where(e => e.IdShort is not null).OrderBy(e => e.IdShort, StringComparer.Ordinal);
}
