using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;

/// <summary>
/// One CRUD action to perform against the target for a specific entity.
/// </summary>
public sealed record CrudDecision(ExportOperation Operation, EntityKind Kind, string Identifier, SourceEntity? SourceEntity);
