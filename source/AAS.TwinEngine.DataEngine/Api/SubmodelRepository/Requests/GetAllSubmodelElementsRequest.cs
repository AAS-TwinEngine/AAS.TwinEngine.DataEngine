namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record GetAllSubmodelElementsRequest(string? SubmodelId, int? Limit, string? Cursor, Level Level, Extent Extent);
