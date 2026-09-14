namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// PostgreSQL state store connection.
/// </summary>
public sealed class StateStoreConfig
{
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Postgres schema name that owns the state tables. Default: <c>export_service</c>.
    /// </summary>
    public string Schema { get; set; } = "export_service";
}
