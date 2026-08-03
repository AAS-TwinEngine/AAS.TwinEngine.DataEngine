using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;

public record GetAllSubmodelElementsByAasRequest(
    string AasIdentifier,
    string SubmodelId,
    int? Limit,
    string? Cursor,
    Level Level,
    Extent Extent);
