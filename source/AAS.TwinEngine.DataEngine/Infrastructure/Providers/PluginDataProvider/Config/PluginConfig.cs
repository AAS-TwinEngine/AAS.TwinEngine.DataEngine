namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

public class PluginConfig
{
    public const string Section = "PluginConfig";

    public const string HttpClientNamePrefix = "plugin-data-provider";

    public const string MetaData = "metadata";

    public required List<Plugin> Plugins { get; set; }
}

public class Plugin
{
    public required string PluginName { get; set; }

    public required Uri PluginUrl { get; set; }
}
