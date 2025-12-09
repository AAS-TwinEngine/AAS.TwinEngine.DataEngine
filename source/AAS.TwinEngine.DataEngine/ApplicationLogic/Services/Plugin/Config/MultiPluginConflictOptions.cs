namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;

public class MultiPluginConflictOptions
{
    public const string Section = "MultiPluginConflictOption";
    public MultiPluginConflictOption HandlingMode { get; set; }

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
