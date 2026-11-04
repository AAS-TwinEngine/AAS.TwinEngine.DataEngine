namespace AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;

/// <summary>
/// Thrown when the target refuses to create an entity because one with the same identifier
/// already exists. Raised instead of a generic failure so the application layer can decide,
/// from its ownership record, whether the existing target entity may be reconciled or must be
/// left untouched.
/// </summary>
public class TargetEntityConflictException : Exception
{
    public TargetEntityConflictException()
    {
    }

    public TargetEntityConflictException(string message)
        : base(message)
    {
    }

    public TargetEntityConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
