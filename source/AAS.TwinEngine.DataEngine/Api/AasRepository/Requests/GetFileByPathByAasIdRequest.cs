namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;

public record GetFileByPathByAasIdRequest(string AasIdentifier, string SubmodelIdentifier, string IdShortPath);
