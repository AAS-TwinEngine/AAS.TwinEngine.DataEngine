namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record GetSubmodelElementRequest(string SubmodelId, string IdShortPath)
{
    public Level? Level { get; set; }

    public Extent? Extent { get; set; }
}
