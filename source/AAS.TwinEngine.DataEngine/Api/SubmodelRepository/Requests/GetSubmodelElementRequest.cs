namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public record GetSubmodelElementRequest(string SubmodelId, string IdShortPath, Level? Level = Level.deep, Extent? Extent = Extent.withoutBlobValue);