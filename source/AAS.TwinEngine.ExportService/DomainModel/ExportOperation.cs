namespace AAS.TwinEngine.ExportService.DomainModel;

/// <summary>
/// The CRUD decision for a single entity in a single run.
/// </summary>
public enum ExportOperation
{
    Skip = 0,
    Create = 1,
    Update = 2,
    Delete = 3
}
