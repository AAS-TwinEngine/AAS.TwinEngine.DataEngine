namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record GetAllSubmodelsRequest(string? SemanticId, string? IdShort, int? Limit, string? Cursor, Level Level, Extent Extent);

public enum Level
{
    deep,
    core
}

public enum Extent
{
    withBlobValue,
    withoutBlobValue
}