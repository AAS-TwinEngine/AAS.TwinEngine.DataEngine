namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public class GetAllSubmodelElementsRequest
{
    public string? SubmodelId { get; init; }

    public int? Limit { get; init; }

    public string? Cursor { get; init; }

    public Level? Level { get; init; }

    public Extent? Extent { get; init; }

    public GetAllSubmodelElementsRequest(string? submodelId, int? limit, string? cursor, Level? level = null, Extent? extent = null)
    {
        SubmodelId = submodelId;
        Limit = limit;
        Cursor = cursor;
        Level = level;
        Extent = extent;
    }
}
