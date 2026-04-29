namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;

/// <summary>
/// Thrown when a plugin rejects the request schema with HTTP 400.
/// This triggers a legacy Draft-07 compatibility retry.
/// </summary>
public class PluginSchemaRejectionException : Exception;
