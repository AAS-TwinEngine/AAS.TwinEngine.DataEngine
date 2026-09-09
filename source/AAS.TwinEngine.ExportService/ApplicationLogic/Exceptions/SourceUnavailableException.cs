namespace AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;

/// <summary>
/// Thrown when the source read for an entity kind fails entirely (after all retries).
/// The runner cancels remaining phases to preserve referential integrity in the target.
/// </summary>
public sealed class SourceUnavailableException : Exception
{
    public SourceUnavailableException(string message) : base(message) { }
    public SourceUnavailableException(string message, Exception inner) : base(message, inner) { }
}
