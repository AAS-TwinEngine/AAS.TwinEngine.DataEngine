namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.LegacyV1;

/// <summary>
/// Thrown when a plugin indicates the request schema is rejected or incompatible,
/// currently via HTTP 400, 404, or 500.
/// </summary>
public class PluginSchemaRejectionException : Exception;
