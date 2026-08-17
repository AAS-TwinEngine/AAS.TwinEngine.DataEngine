using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;

public record GetSubmodelElementByAasRequest(string AasIdentifier, string SubmodelId, string IdShortPath, Level Level, Extent Extent);
