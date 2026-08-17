namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record GetSubmodelRequest(string submodelId, Level? level = Level.deep, Extent? extent = Extent.withoutBlobValue);
