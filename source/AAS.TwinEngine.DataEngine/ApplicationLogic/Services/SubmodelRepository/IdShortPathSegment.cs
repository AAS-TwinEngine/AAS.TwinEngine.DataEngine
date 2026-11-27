using System.Globalization;
using System.Text.RegularExpressions;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

/// <summary>
/// One dot-separated segment of an idShortPath: an idShort followed by zero or more list indices,
/// e.g. <c>Prop</c>, <c>List[2]</c> or <c>Matrix[0][1]</c> (indices may also be URL-encoded as <c>%5B0%5D</c>).
/// </summary>
public sealed partial record IdShortPathSegment(string IdShort, IReadOnlyList<int> Indices)
{
    public bool HasIndices => Indices.Count > 0;

    public static IdShortPathSegment Parse(string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);

        var match = SegmentPattern().Match(segment);
        if (!match.Success)
        {
            return new IdShortPathSegment(segment, []);
        }

        var indices = match.Groups["index"].Captures
                           .Select(capture => int.Parse(capture.Value, CultureInfo.InvariantCulture))
                           .ToList();

        return new IdShortPathSegment(match.Groups["idShort"].Value, indices);
    }

    [GeneratedRegex(@"^(?<idShort>.+?)(?:\[(?<index>\d+)\]|%5B(?<index>\d+)%5D)+$")]
    private static partial Regex SegmentPattern();
}
