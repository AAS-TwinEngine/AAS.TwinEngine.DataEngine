using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_0;

using Microsoft.Extensions.Options;

using File = AasCore.Aas3_0.File;
using Range = AasCore.Aas3_0.Range;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public partial class SemanticIdResolver(IOptions<PluginsConfig> pluginsConfig, IOptions<TemplateManagementConfig> templateManagementConfig) : ISemanticIdResolver
{
    public const string RangeMinimumPostFixSeparator = "_min";
    public const string RangeMaximumPostFixSeparator = "_max";
    public const string EntityGlobalAssetIdPostFix = "_globalAssetId";
    public const string RelationshipElementFirstPostFixSeparator = "_first";
    public const string RelationshipElementSecondPostFixSeparator = "_second";
    private readonly string _submodelElementIndexContextPrefix = pluginsConfig.Value.SubmodelElementIndexContextPrefix;

    public string MlpPostFixSeparator { get; } = pluginsConfig.Value.MultiLanguageProperty.SemanticPostfixSeparator;

    public string InternalSemanticIdType { get; } = templateManagementConfig.Value.Semantics.InternalSemanticId;

    private static readonly HashSet<DataTypeDefXsd> StringTypes =
    [
        DataTypeDefXsd.String, DataTypeDefXsd.AnyUri, DataTypeDefXsd.Byte, DataTypeDefXsd.Date,
        DataTypeDefXsd.DateTime, DataTypeDefXsd.Duration, DataTypeDefXsd.GDay, DataTypeDefXsd.GYear,
        DataTypeDefXsd.GYearMonth, DataTypeDefXsd.HexBinary, DataTypeDefXsd.Time, DataTypeDefXsd.Base64Binary,
        DataTypeDefXsd.GMonth, DataTypeDefXsd.GMonthDay
    ];

    private static readonly HashSet<DataTypeDefXsd> IntegerTypes =
    [
        DataTypeDefXsd.Int, DataTypeDefXsd.Integer, DataTypeDefXsd.Long, DataTypeDefXsd.NegativeInteger,
        DataTypeDefXsd.NonNegativeInteger, DataTypeDefXsd.NonPositiveInteger, DataTypeDefXsd.PositiveInteger,
        DataTypeDefXsd.Short, DataTypeDefXsd.UnsignedShort, DataTypeDefXsd.UnsignedLong,
        DataTypeDefXsd.UnsignedInt, DataTypeDefXsd.UnsignedByte
    ];

    private static readonly HashSet<DataTypeDefXsd> NumberTypes =
    [
        DataTypeDefXsd.Float, DataTypeDefXsd.Double, DataTypeDefXsd.Decimal
    ];

    public string GetSemanticId(IHasSemantics hasSemantics) => hasSemantics.SemanticId?.Keys?.FirstOrDefault()?.Value ?? string.Empty;

    public string ExtractSemanticId(ISubmodelElement element)
    {
        if (element.Qualifiers == null)
        {
            return GetSemanticId(element);
        }

        var qualifier = element.Qualifiers.FirstOrDefault(q => q.Type == InternalSemanticIdType);
        return qualifier != null ? qualifier.Value! : GetSemanticId(element);
    }

    public string ResolveSemanticId(IHasSemantics hasSemantics, string idShort)
    {
        var baseSemanticId = GetSemanticId(hasSemantics);
        return AppendIndex(baseSemanticId, idShort);
    }

    public string ResolveElementSemanticId(ISubmodelElement element, string idShort)
    {
        var baseSemanticId = ExtractSemanticId(element);
        return AppendIndex(baseSemanticId, idShort);
    }

    public Cardinality GetCardinality(ISubmodelElement element)
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

    public DataType GetValueType(ISubmodelElement element)
    {
        return element switch
        {
            Property p => GetDataTypeFromValueType(p.ValueType),
            Range r => GetDataTypeFromValueType(r.ValueType),
            File => DataType.String,
            Blob => DataType.String,
            _ => DataType.Unknown
        };
    }

    private static DataType GetDataTypeFromValueType(DataTypeDefXsd valueType)
    {
        return valueType switch
        {
            _ when StringTypes.Contains(valueType) => DataType.String,
            _ when IntegerTypes.Contains(valueType) => DataType.Integer,
            _ when NumberTypes.Contains(valueType) => DataType.Number,
            DataTypeDefXsd.Boolean => DataType.Boolean,
            _ => DataType.Unknown
        };
    }

    public string BuildReferenceKeySemanticId(string baseSemanticId, KeyTypes keyType, int index, int totalCount)
    {
        return totalCount > 1
                   ? $"{baseSemanticId}{MlpPostFixSeparator}{keyType}{MlpPostFixSeparator}{index}"
                   : $"{baseSemanticId}{MlpPostFixSeparator}{keyType}";
    }

    private string AppendIndex(string semanticId, string? idShort)
    {
        var index = string.Empty;
        if (idShort != null)
        {
            index = SubmodelElementCollectionIndex().Match(idShort).Value;
        }

        return string.IsNullOrWhiteSpace(index)
                   ? semanticId
                   : $"{semanticId}{_submodelElementIndexContextPrefix}{index}";
    }

    /// <summary>
    /// Matches one or more digits at the end of a string,
    /// e.g., "element42" → matches "42"
    /// Pattern: \d+$
    /// </summary>
    [GeneratedRegex(@"\d+$")]
    private static partial Regex SubmodelElementCollectionIndex();
}
