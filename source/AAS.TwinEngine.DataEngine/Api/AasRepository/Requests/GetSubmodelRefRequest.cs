namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;

public record GetSubmodelRefRequest(string AasIdentifier, int? Limit, string? Cursor);
