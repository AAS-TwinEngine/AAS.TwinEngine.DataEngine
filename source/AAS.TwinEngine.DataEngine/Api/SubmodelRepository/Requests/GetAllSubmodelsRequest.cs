namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record GetAllSubmodelsRequest
{
    public string? SemanticId { get; set; }

    public string? IdShort { get; set; }

    public int? Limit { get; set; }

    public string? Cursor { get; set; }

    public Level? Level { get; set; }

    public Extent? Extent { get; set; }
}

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
