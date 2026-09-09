using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;

public sealed class CrudDecisionMaker : ICrudDecisionMaker
{
    public IReadOnlyList<CrudDecision> Decide(
        EntityKind kind,
        IReadOnlyList<SourceEntity> sourceEntities,
        IReadOnlyList<ExportedEntity> managedState)
    {
        var stateByIdentifier = managedState.ToDictionary(e => e.Identifier, StringComparer.Ordinal);
        var sourceIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        var decisions = new List<CrudDecision>(sourceEntities.Count + managedState.Count);

        foreach (var source in sourceEntities)
        {
            _ = sourceIdentifiers.Add(source.Identifier);

            if (!stateByIdentifier.ContainsKey(source.Identifier))
            {
                decisions.Add(new CrudDecision(ExportOperation.Create, kind, source.Identifier, source));
            }
            else
            {
                decisions.Add(new CrudDecision(ExportOperation.Update, kind, source.Identifier, source));
            }
        }

        foreach (var managed in managedState.Where(m => !sourceIdentifiers.Contains(m.Identifier)))
        {
            decisions.Add(new CrudDecision(ExportOperation.Delete, kind, managed.Identifier, null));
        }

        return decisions;
    }
}
