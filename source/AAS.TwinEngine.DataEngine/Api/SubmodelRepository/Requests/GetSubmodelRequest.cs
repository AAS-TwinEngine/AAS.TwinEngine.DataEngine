namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record GetSubmodelRequest(string SubmodelId, Level? Level = Level.deep, Extent? Extent = Extent.withoutBlobValue);
