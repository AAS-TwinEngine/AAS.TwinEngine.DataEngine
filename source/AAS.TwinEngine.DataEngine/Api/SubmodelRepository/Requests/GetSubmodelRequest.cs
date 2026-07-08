namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public class GetSubmodelRequest(string submodelId)
{
    public string SubmodelId { get; } = submodelId;

    /// <summary>
    /// Determines the structural depth of the resource content.
    /// </summary>
    public Level? Level { get; set; }

    /// <summary>
    /// Determines to which extent the resource is serialized.
    /// </summary>
    public Extent? Extent { get; set; }
}
