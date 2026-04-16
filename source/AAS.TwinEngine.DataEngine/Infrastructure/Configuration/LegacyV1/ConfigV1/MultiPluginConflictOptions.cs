namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]

public class MultiPluginConflictOptions
{
    public const string Section = "MultiPluginConflictOption";
    public MultiPluginConflictOption HandlingMode { get; set; } = MultiPluginConflictOption.ThrowError;

    public enum MultiPluginConflictOption
    {
        /// <summary>
        /// Throw an exception and stop processing if conflicting values are found.
        /// </summary>
        ThrowError,

        /// <summary>
        /// Ignore the conflicting semanticId. When requested, the submodel returns null.
        /// </summary>
        SkipConflictingIds,

        /// <summary>
        /// Use the value from the first plugin encountered and ignore later conflicting values.
        /// </summary>
        TakeFirst,
    }
}
