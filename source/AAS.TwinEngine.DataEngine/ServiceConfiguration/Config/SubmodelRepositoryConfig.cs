namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

public class SubmodelRepositoryConfig
{
    public const string Section = "SubmodelRepository";

    /// <summary>
    /// Maximum allowed file size in bytes for attachment downloads.
    /// Defaults to 100 MB. Set to 0 to disable the limit.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;
}
