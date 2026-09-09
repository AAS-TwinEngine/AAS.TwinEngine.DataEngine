using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;

/// <summary>
/// Compares the current source snapshot to the exporter's state store and returns the set
/// of CRUD actions that must be applied to the target.
/// </summary>
public interface ICrudDecisionMaker
{
    IReadOnlyList<CrudDecision> Decide(
        EntityKind kind,
        IReadOnlyList<SourceEntity> sourceEntities,
        IReadOnlyList<ExportedEntity> managedState);
}
