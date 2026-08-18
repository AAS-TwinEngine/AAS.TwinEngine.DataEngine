using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;

public record GetSubmodelByAasRequest(string AasIdentifier, string SubmodelId, Level Level, Extent Extent);
