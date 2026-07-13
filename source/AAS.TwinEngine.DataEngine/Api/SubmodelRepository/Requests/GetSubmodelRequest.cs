namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public class GetSubmodelRequest(string submodelId)
{
    public string SubmodelId { get; } = submodelId;

    public Level? Level { get; set; }

    public Extent? Extent { get; set; }
}
